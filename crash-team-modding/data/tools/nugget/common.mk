PREFIX ?= $(MIPS_PREFIX)
FORMAT ?= elf32-littlemips

ROOTDIR = $(NUGGET_F)

CC  = $(PREFIX)-gcc
CXX = $(PREFIX)-g++

OVR_START_SYM = -Xlinker --defsym=OVR_START_ADDR=$(OVR_START_ADDR)
LDFLAGS += -Wl,--gc-sections

ARCHFLAGS = -march=mips1 -mabi=32 -EL -fno-pic -mno-shared -mno-abicalls -mfp32
ARCHFLAGS += -fno-stack-protector -nostdlib -ffreestanding
CPPFLAGS += -ffunction-sections -fdata-sections
ifeq ($(DISABLE_FUNCTION_REORDER),true)
CPPFLAGS += -fno-toplevel-reorder
endif
CPPFLAGS += -mno-gpopt -fomit-frame-pointer
CPPFLAGS += -fno-builtin -fno-strict-aliasing -Wno-attributes -Wextra
CPPFLAGS += $(ARCHFLAGS)
CPPFLAGS += -I$(ROOTDIR)

LDFLAGS += -Wl,-Map=$(BINDIR)$(TARGET).map -nostdlib -T$(OVERLAYSCRIPT) $(LDSYMS) -static
LDFLAGS += $(ARCHFLAGS) -Wl,--oformat=$(FORMAT)
LDFLAGS += $(OVR_START_SYM)

CPPFLAGS += $(EXTRA_CC_FLAGS)
LDFLAGS += $(EXTRA_CC_FLAGS)
CPPFLAGS += $(OPT_CC_FLAGS)
LDFLAGS += $(OPT_LD_FLAGS)

CXXFLAGS += -fno-exceptions -fno-rtti

OBJS += $(addsuffix .o, $(basename $(SRCS)))

DEPS := $(patsubst %.cpp, %.dep,$(filter %.cpp,$(SRCS)))
DEPS += $(patsubst %.cc, %.dep,$(filter %.cc,$(SRCS)))
DEPS +=	$(patsubst %.c, %.dep,$(filter %.c,$(SRCS)))
DEPS += $(patsubst %.s, %.dep,$(filter %.s,$(SRCS)))

# USE PORTABLE PYTHON
all: pch dep $(foreach ovl, $(OVERLAYSECTION), $(BINDIR)Overlay$(ovl))

$(BINDIR)Overlay%: $(BINDIR)$(TARGET).elf
	$(PREFIX)-objcopy -j $(@:$(BINDIR)Overlay%=%) -O binary $< $(BINDIR)$(TARGET)$(@:$(BINDIR)Overlay%=%)
	$(PYTHON_P) $(TRIMBINPY) $(BINDIR)$(TARGET)$(@:$(BINDIR)Overlay%=%) $(BUILDDIR) $(TRIMBIN_OFFSET)

$(BINDIR)$(TARGET).elf: $(OBJS)
ifneq ($(strip $(BINDIR)),)
	mkdir -p $(BINDIR)
endif
	$(CC) -o $(BINDIR)$(TARGET).elf $(OBJS) $(LDFLAGS)

$(info CC=$(CC))
$(info TARGET=$(TARGET))
$(info OBJS=$(OBJS))
$(info LDFLAGS=$(LDFLAGS))



%.o: %.s
	$(CC) $(ARCHFLAGS) -I$(ROOTDIR) -c $< -o $@

%.dep: %.c
	$(CC) $(CPPFLAGS) $(CFLAGS) -M -MT $(addsuffix .o, $(basename $@)) -MF $@ $<

%.dep: %.cpp
	$(CXX) $(CPPFLAGS) $(CXXFLAGS) -M -MT $(addsuffix .o, $(basename $@)) -MF $@ $<

%.dep: %.cc
	$(CXX) $(CPPFLAGS) $(CXXFLAGS) -M -MT $(addsuffix .o, $(basename $@)) -MF $@ $<

%.dep: %.s
	$(GCCDIR)touch $@

dep: $(DEPS)

%.h.gch: %.h
	$(CC) $(CPPFLAGS) $(CFLAGS) -c $< -o $@

pch: $(PCHS)

-include $(DEPS)

.PHONY: all pch dep