PYTHON_P = $(PYTHON_PORTABLE)
MIPS_PREFIX = $(MIPS_P)
TRIMBINPY = $(TRIMBIN_F)
THISDIR := $(dir $(abspath $(lastword $(MAKEFILE_LIST))))
TOOLSDIR = $(TOOLS_PATH)
NUGGET_F = $(NUGGET_FOLDER)

CPPFLAGS += -I$(NUGGET_MACROS)
CPPFLAGS += -I$(GAMEINCLUDEDIR)
CPPFLAGS += -I$(MODDIR)

ifeq ($(USE_MININOOB),true)
  CPPFLAGS += -I$(MINI_INCLUDE)
  LDFLAGS += -L$(MINI_LIB)minin00b/lib/
  LDFLAGS += -Wl,--start-group
  LDFLAGS += -l:libc.a
  LDFLAGS += -l:psxcd.a
  LDFLAGS += -l:psxetc.a
  LDFLAGS += -l:psxgpu.a
  LDFLAGS += -l:psxgte.a
  LDFLAGS += -l:psxpress.a
  LDFLAGS += -l:psxsio.a
  LDFLAGS += -l:psxspu.a
  LDFLAGS += -l:psxapi.a
  LDFLAGS += -Wl,--end-group
endif

ifeq ($(USE_PSYQ),true)
  CPPFLAGS += -I$(PSYQ_INCLUDE)
  LDFLAGS += -L$(PSYQ_LIB)
  LDFLAGS += -Wl,--start-group
  LDFLAGS += -lapi
  LDFLAGS += -lc
  LDFLAGS += -lc2
  LDFLAGS += -lcard
  LDFLAGS += -lcomb
  LDFLAGS += -lds
  LDFLAGS += -letc
  LDFLAGS += -lgpu
  LDFLAGS += -lgs
  LDFLAGS += -lgte
  LDFLAGS += -lgun
  LDFLAGS += -lhmd
  LDFLAGS += -lmath
  LDFLAGS += -lmcrd
  LDFLAGS += -lmcx
  LDFLAGS += -lpad
  LDFLAGS += -lpress
  LDFLAGS += -lsio
  LDFLAGS += -lsnd
  LDFLAGS += -lspu
  LDFLAGS += -ltap
  LDFLAGS += -lcd
  LDFLAGS += -Wl,--end-group
endif

include $(NUGGET_COMMON)