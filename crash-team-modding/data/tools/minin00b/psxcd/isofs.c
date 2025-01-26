
#undef  SDK_LIBRARY_NAME
#define SDK_LIBRARY_NAME "psxcd/iso"

#include <stdint.h>
#include <stdlib.h>
#include <string.h>
#include <assert.h>
#include <psxgpu.h>
#include <psxapi.h>
#include <psxcd.h>
#include "isofs.h"

#define CD_READ_ATTEMPTS	3
#define DEFAULT_PATH_SEP	'\\'
#define IS_PATH_SEP(ch)		(((ch) == '/') || ((ch) == '\\'))

extern volatile int _cd_media_changed;

static int			_cd_iso_last_dir_lba;
static uint8_t		_cd_iso_descriptor_buff[2048];
static uint8_t		_cd_iso_pathtable_buff[2048];
static uint8_t		_cd_iso_directory_buff[2048];
static int			_cd_iso_directory_len;
static CdlIsoError	_cd_iso_error=CdlIsoOkay;

static int _CdReadIsoDescriptor(int session_offs)
{
	CdlLOC loc;
	ISO_DESCRIPTOR *descriptor;

	// Check if the lid had been opened
	if( !_cd_media_changed )
	{
		CdControl(CdlNop, 0, 0);
		if( (CdStatus()&0x10) )
		{
			// Check if lid is still open
			CdControl(CdlNop, 0, 0);
			if( (CdStatus()&0x10) )
			{
				_sdk_log("Lid is still open.\n");

				_cd_iso_error = CdlIsoLidOpen;
				return -1;
			}
			// Reparse the file system
			_cd_media_changed = 1;
		}
	}

	if( !_cd_media_changed )
	{
		return 0;
	}

	_sdk_log("Parsing ISO file system.\n");

	// Seek to volume descriptor
	CdIntToPos(16+session_offs, &loc);
	if( !CdControl(CdlSetloc, (uint8_t*)&loc, 0) )
	{
		_sdk_log("Could not set seek destination.\n");

		_cd_iso_error = CdlIsoSeekError;
		return -1;
	}

	_sdk_log("Read sectors.\n");

	// Read volume descriptor
	CdReadRetry(1, (uint32_t*)_cd_iso_descriptor_buff, CdlModeSpeed, CD_READ_ATTEMPTS);

	if( CdReadSync(0, 0) )
	{
		_sdk_log("Error reading ISO volume descriptor.\n");

		_cd_iso_error = CdlIsoReadError;
		return -1;
	}

	_sdk_log("Read complete.\n");

	// Verify if volume descriptor is present
	descriptor = (ISO_DESCRIPTOR*)_cd_iso_descriptor_buff;
	if( memcmp("CD001", descriptor->header.id, 5) )
	{
		_sdk_log("Disc does not contain a ISO9660 file system.\n");

		_cd_iso_error = CdlIsoInvalidFs;
		return -1;
	}

	_sdk_log("Path table LBA = %d\n", descriptor->pathTable1Offs);
	_sdk_log("Path table len = %d\n", descriptor->pathTableSize.lsb);

	// Read path table
	CdIntToPos(descriptor->pathTable1Offs, &loc);
	CdControl(CdlSetloc, (uint8_t*)&loc, 0);
	CdReadRetry(1, (uint32_t*)_cd_iso_pathtable_buff, CdlModeSpeed, CD_READ_ATTEMPTS);
	if( CdReadSync(0, 0) )
	{
		_sdk_log("Error reading ISO path table.\n");

		_cd_iso_error = CdlIsoReadError;
		return -1;
	}

	_cd_iso_last_dir_lba	= 0;
	_cd_iso_error			= CdlIsoOkay;

	_cd_media_changed		= 0;

	return 0;
}

static int _CdReadIsoDirectory(int lba)
{
	int i;
	CdlLOC loc;
	ISO_DIR_ENTRY *direntry;

	if( lba == _cd_iso_last_dir_lba )
	{
		return 0;
	}

	CdIntToPos(lba, &loc);
	i = CdPosToInt(&loc);

	_sdk_log("Seek to sector %d\n", i);

	if( !CdControl(CdlSetloc, (uint8_t*)&loc, 0) )
	{
		_sdk_log("Could not set seek destination.\n");

		_cd_iso_error = CdlIsoSeekError;
		return -1;
	}

	CdReadRetry(1, (uint32_t*)_cd_iso_directory_buff, CdlModeSpeed, CD_READ_ATTEMPTS);
	if( CdReadSync(0, 0) )
	{
		_sdk_log("Error reading initial directory record.\n");

		_cd_iso_error = CdlIsoReadError;
		return -1;
	}

	direntry = (ISO_DIR_ENTRY*)_cd_iso_directory_buff;
	_cd_iso_directory_len = direntry->entrySize.lsb;

	_sdk_log("Location of directory record = %d\n", direntry->entryOffs.lsb);
	_sdk_log("Size of directory record = %d\n", _cd_iso_directory_len);

	_cd_iso_last_dir_lba = lba;
	_cd_iso_error = CdlIsoOkay;

	return 0;
}

#if 0

static void dump_directory(void)
{
	int i;
	int dir_pos;
	ISO_DIR_ENTRY *dir_entry;
	char namebuff[16];

	_sdk_log("Cached directory record contents:\n");

	i = 0;
	dir_pos = 0;
	while(1)
	{
		dir_entry = (ISO_DIR_ENTRY*)(_cd_iso_directory_buff+dir_pos);

		memcpy(
			namebuff,
			_cd_iso_directory_buff+dir_pos+sizeof(ISO_DIR_ENTRY),
			dir_entry->identifierLen
		);
		namebuff[dir_entry->identifierLen] = 0;

		_sdk_log("P:%d L:%d %s\n", dir_pos, dir_entry->identifierLen, namebuff);

		dir_pos += dir_entry->entryLength;
		i++;

		// Check if padding is reached (end of record sector)
		if( _cd_iso_directory_buff[dir_pos] == 0 )
		{
			// Snap it to next sector
			dir_pos = ((dir_pos+2047)>>11)<<11;

			// Break if exceeds length of directory buffer (end)
			if( dir_pos >= _cd_iso_directory_len )
			{
				break;
			}
		}
	}

	_sdk_log("--\n");

}

static void dump_pathtable(void)
{
	uint8_t *tbl_pos;
	ISO_PATHTABLE_ENTRY *tbl_entry;
	ISO_DESCRIPTOR *descriptor;
	char namebuff[16];

	_sdk_log("Path table entries:\n");

	descriptor = (ISO_DESCRIPTOR*)_cd_iso_descriptor_buff;

	tbl_pos = _cd_iso_pathtable_buff;
	tbl_entry = (ISO_PATHTABLE_ENTRY*)tbl_pos;

	while( (int)(tbl_pos-_cd_iso_pathtable_buff) <
		descriptor->pathTableSize.lsb )
	{
		memcpy(
			namebuff,
			tbl_pos+sizeof(ISO_PATHTABLE_ENTRY),
			tbl_entry->nameLength
		);
		namebuff[tbl_entry->nameLength] = 0;

		_sdk_log("%s\n", namebuff);

		// Advance to next entry
		tbl_pos += sizeof(ISO_PATHTABLE_ENTRY)
			+(2*((tbl_entry->nameLength+1)/2));

		tbl_entry = (ISO_PATHTABLE_ENTRY*)tbl_pos;
	}

}

#endif

static int get_pathtable_entry(int entry, ISO_PATHTABLE_ENTRY *tbl, char *namebuff)
{
	int i;
	uint8_t *tbl_pos;
	ISO_PATHTABLE_ENTRY *tbl_entry;
	ISO_DESCRIPTOR *descriptor;

	descriptor = (ISO_DESCRIPTOR*)_cd_iso_descriptor_buff;

	tbl_pos = _cd_iso_pathtable_buff;
	tbl_entry = (ISO_PATHTABLE_ENTRY*)tbl_pos;

	i = 0;
	while( (int)(tbl_pos-_cd_iso_pathtable_buff) <
		descriptor->pathTableSize.lsb )
	{
		if( i == (entry-1) )
		{
			if( namebuff )
			{
				memcpy(
					namebuff,
					tbl_pos+sizeof(ISO_PATHTABLE_ENTRY),
					tbl_entry->nameLength
				);
				namebuff[tbl_entry->nameLength] = 0;
			}

			if( tbl )
			{
				*tbl = *tbl_entry;
			}

			return 0;
		}

		// Advance to next entry
		tbl_pos += sizeof(ISO_PATHTABLE_ENTRY)
			+(2*((tbl_entry->nameLength+1)/2));

		tbl_entry = (ISO_PATHTABLE_ENTRY*)tbl_pos;
		i++;
	}

	if( entry <= 0 )
	{
		return i+1;
	}

	return -1;
}

static char* resolve_pathtable_path(int entry, char *rbuff)
{
	char namebuff[16];
	ISO_PATHTABLE_ENTRY tbl_entry;

	*rbuff = 0;

	do
	{
		if( get_pathtable_entry(entry, &tbl_entry, namebuff) )
		{
			return NULL;
		}

		rbuff -= tbl_entry.nameLength;
		memcpy(rbuff, namebuff, tbl_entry.nameLength);
		rbuff--;
		*rbuff = DEFAULT_PATH_SEP;

		// Parse to the parent
		entry = tbl_entry.dirLevel;

	} while( entry > 1 );

	return rbuff;
}

static int find_dir_entry(const char *name, ISO_DIR_ENTRY *dirent)
{
	int i;
	int dir_pos;
	ISO_DIR_ENTRY *dir_entry;
	char namebuff[16];

	_sdk_log("Locating file %s.\n", name);

	i = 0;
	dir_pos = 0;
	while(dir_pos < _cd_iso_directory_len)
	{
		dir_entry = (ISO_DIR_ENTRY*)(_cd_iso_directory_buff+dir_pos);

		if( !(dir_entry->flags & 0x2) )
		{
			memcpy(
				namebuff,
				_cd_iso_directory_buff+dir_pos+sizeof(ISO_DIR_ENTRY),
				dir_entry->identifierLen
			);
			namebuff[dir_entry->identifierLen] = 0;

			if( strcmp(namebuff, name) == 0 )
			{
				*dirent = *dir_entry;
				return 0;
			}
		}

		dir_pos += dir_entry->entryLength;
		i++;

		// Check if padding is reached (end of record sector)
		if( _cd_iso_directory_buff[dir_pos] == 0 )
		{
			// Snap it to next sector
			dir_pos = ((dir_pos+2047)>>11)<<11;

		}
	}

	return -1;
}

static char* get_pathname(char *path, const char *filename)
{
	const char *c = 0;
	for (const char *i = filename; *i; i++) {
		if (IS_PATH_SEP(*i))
			c = i;
	}

	if(( c == filename ) || ( !c ))
	{
		path[0] = DEFAULT_PATH_SEP;
		path[1] = 0;
		return NULL;
	}

	memcpy(path, filename, c - filename);
	path[c - filename] = 0;
	return path;
}

static char* get_filename(char *name, const char *filename)
{
	const char *c = 0;
	for (const char *i = filename; *i; i++) {
		if (IS_PATH_SEP(*i))
			c = i;
	}

	if (!c) {
		strcpy(name, filename);
		return name;
	}
	if (c == filename) {
		strcpy(name, filename+1);
		return name;
	}

	c++;
	strcpy(name, c);
	return name;
}

CdlFILE *CdSearchFile(CdlFILE *fp, const char *filename)
{
	_sdk_validate_args(fp && filename, NULL);

	int i,j,found_dir,num_dirs;
	int dir_len;
	char tpath_rbuff[128];
	char search_path[128];
	char *rbuff;
	ISO_PATHTABLE_ENTRY tbl_entry;
	ISO_DIR_ENTRY dir_entry;

	// Read ISO descriptor if changed flag is set
	//if( _cd_media_changed )
	//{
		// Read ISO descriptor and path table
	if( _CdReadIsoDescriptor(0) )
	{
		_sdk_log("Could not read ISO file system.\n");
		return NULL;
	}

	//	_sdk_log("ISO file system cache updated.\n");
	//	_cd_media_changed = 0;
	//}

	// Get number of directories in path table
	num_dirs = get_pathtable_entry(0, NULL, NULL);

#ifndef NDEBUG
	_sdk_log("Directories in path table: %d\n", num_dirs);

	rbuff = resolve_pathtable_path(num_dirs-1, tpath_rbuff+127);

	if( !rbuff )
	{
		_sdk_log("Could not resolve path.\n");
	}
	else
	{
		_sdk_log("Longest path: %s\n", rbuff);
	}
#endif

	if( get_pathname(search_path, filename) )
	{
		_sdk_log("Search path = %s\n", search_path);
	}

	// Search the pathtable for a matching path
	found_dir = 0;
	for(i=1; i<num_dirs; i++)
	{
		rbuff = resolve_pathtable_path(i, tpath_rbuff+127);
		_sdk_log("Found = %s\n", rbuff);

		if( rbuff )
		{
			if( strcmp(search_path, rbuff) == 0 )
			{
				found_dir = i;
				break;
			}
		}
	}

	if( !found_dir )
	{
		_sdk_log("Directory path not found.\n");
		return NULL;
	}

	_sdk_log("Found directory at record %d!\n", found_dir);

	get_pathtable_entry(found_dir, &tbl_entry, NULL);
	_sdk_log("Directory LBA = %d\n", tbl_entry.dirOffs);

	_CdReadIsoDirectory(tbl_entry.dirOffs);
	get_filename(fp->name, filename);

	// Add version number if not specified
	if( !strchr(fp->name, ';') )
	{
		strcat(fp->name, ";1");
	}

#ifndef NDEBUG
	//dump_directory();
#endif

	if( find_dir_entry(fp->name, &dir_entry) )
	{
		_sdk_log("Could not find file.\n");

		return NULL;
	}

	_sdk_log("Located file at LBA %d.\n", dir_entry.entryOffs.lsb);

	CdIntToPos(dir_entry.entryOffs.lsb, &fp->pos);
	fp->size = dir_entry.entrySize.lsb;

	return fp;
}

CdlIsoError CdIsoError()
{
	return _cd_iso_error;
}

int CdGetVolumeLabel(char *label)
{
	_sdk_validate_args(label, -1);

	int i, length = 31;
	ISO_DESCRIPTOR* descriptor;

	if( _CdReadIsoDescriptor(0) )
		return -1;

	descriptor = (ISO_DESCRIPTOR*)_cd_iso_descriptor_buff;

	while (descriptor->volumeID[length] == 0x20)
		length--;

	length++;
	memcpy(label, descriptor->volumeID, length);
	label[length] = 0x00;

	return length;
}


// Session load routine

static volatile unsigned int _ready_oldcb;

static volatile int _ses_scanfound;
static volatile int _ses_scancount;
static volatile int _ses_scancomplete;
//static volatile char _ses_scan_resultbuff[8];
static volatile char *_ses_scanbuff;

static void _scan_callback(CdlIntrResult status, unsigned char *result)
{
	if( status == CdlDataReady )
	{
		CdGetSector((void*)_ses_scanbuff, 512);

		if( _ses_scanbuff[0] == 0x1 )
		{
			if( memcmp((const char*)_ses_scanbuff+1, "CD001", 5) == 0 )
			{
				CdControlF(CdlPause, 0);
				_ses_scancomplete = 1;
				_ses_scanfound = 1;
				return;
			}
		}
		_ses_scancount++;
		if( _ses_scancount >= 512 )
		{
			CdControlF(CdlPause, 0);
			_ses_scancomplete = 1;
			return;
		}
	}

	if( status == CdlDiskError )
	{
		CdControlF(CdlPause, 0);
		_ses_scancomplete = 1;
	}
}

int CdLoadSession(int session)
{
	_sdk_validate_args(session >= 0, -1);

	CdlLOC *loc;
	CdlCB ready_oldcb;
	char scanbuff[2048];
	char resultbuff[16];
	int i;

	// Seek to specified session
	_sdk_log("CdLoadSession(): Seeking to session %d...\n", session);
	CdControl(CdlSetsession, (unsigned char*)&session,
		(unsigned char*)&resultbuff);

	if( CdSync(0, 0) == CdlDiskError )
	{
		_sdk_log("CdLoadSession(): Session seek failed, session does not exist. Restarting CD-ROM...\n");

		// Restart CD-ROM on session seek failure
		CdControl(CdlNop, 0, 0);
		CdControl(CdlInit, 0, 0);
		CdSync(0, 0);

		return -1;
	}

	// Set search routine callback
	ready_oldcb = CdReadyCallback(_scan_callback);

	_ses_scanfound = 0;
	_ses_scancount = 0;
	_ses_scancomplete = 0;
	_ses_scanbuff = scanbuff;

	// Begin scan for an ISO volume descriptor
	_sdk_log("CdLoadSession(): Scanning for ISO9660 volume descriptor.\n");

	i = CdlModeSpeed;
	CdControl(CdlSetmode, (unsigned char*)&i, 0);
	CdControl(CdlReadN, 0, (unsigned char*)resultbuff);

	// Wait until scan complete
	while(!_ses_scancomplete);

	CdReadyCallback((void*)_ready_oldcb);

	if( !_ses_scanfound )
	{
		_sdk_log("CdLoadSession(): Did not find volume descriptor.\n");

		_cd_iso_error = CdlIsoInvalidFs;
		CdReadyCallback((CdlCB)ready_oldcb);

		return -1;
	}

	// Restore old callback if any
	CdReadyCallback((CdlCB)ready_oldcb);

	// Wait until CD-ROM has completely stopped reading, to get a consistent
	// fix of the CD-ROM pickup's current location
	do
	{
		VSync(2);
		CdControl(CdlNop, 0, 0);
	} while(CdStatus()&0xE0);

	// Get location of volume descriptor
	CdControl(CdlGetlocL, 0, (unsigned char*)resultbuff);
	CdSync(0, 0);

	loc = (CdlLOC*)resultbuff;

	_sdk_log("CdLoadSession(): Session found in %02d:%02d:%02d (LBA=%d)\n",
		btoi(loc->minute), btoi(loc->second), btoi(loc->sector), CdPosToInt(loc));

	i = CdPosToInt(loc)-17;
	_sdk_log("CdLoadSession(): Session starting at LBA=%d\n", i);

	_cd_media_changed = 1;

	if( _CdReadIsoDescriptor(i) )
	{
		return -1;
	}

	return 0;
}
