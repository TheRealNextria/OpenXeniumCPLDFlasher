# OpenXenium CPLD Flasher

A Windows GUI for programming the **Xilinx XC9572XL CPLD** used by the **OpenXenium** Xbox modchip.

The application supports:

- **xFlasher 360**
- **Xilinx Platform Cable USB**

It provides device detection, CPLD programming, erase support, progress indication, and logging.

## Features

- Windows .NET 8 WinForms application
- Detects the OpenXenium **XC9572XL**
- Supports **xFlasher 360**
- Supports **Xilinx Platform Cable USB**
- Automatic Xilinx USB cable firmware initialization
- Programs `.SVF` files using xFlasher
- Programs `.JED` files using Xilinx Platform Cable USB
- CPLD erase function for Xilinx Platform Cable USB
- Automatic USB device selection
- Programming progress and detailed log output
- Tested stable Xilinx JTAG frequency: **750 kHz**

## Supported CPLD

```text
Xilinx XC9572XL-10VQG64C
```

Used by the OpenXenium Xbox modchip.

---

## Supported Programmers

### xFlasher 360

The xFlasher backend uses a modified version of `xsvftool`.

Detection:

```text
xsvftool.exe -A -j 0 -c
```

Programming:

```text
xsvftool.exe -A -j 0 -p -s "openxenium.svf"
```

The tested xsvftool build uses a maximum JTAG programming frequency of approximately **500 kHz**.

Typical programming time:

```text
~6 seconds
```

Firmware format:

```text
.SVF
```

---

### Xilinx Platform Cable USB

The Xilinx backend uses a modified build of **openFPGALoader** with support for the original Xilinx Platform Cable USB.

Initialized cable:

```text
VID: 03FD
PID: 0008
```

Cold/uninitialized cable:

```text
03FD:000F
```

The application automatically initializes the cable before CPLD detection or programming.

#### Automatic cable initialization

```text
03FD:000F
    ↓
Load xusb_xlp_bootstrap_extracted.hex
    ↓
03FD:0008
    ↓
Load xusb_xlp.hex
    ↓
Load xusb_xlp.hex again
    ↓
Cable ready
```

The runtime firmware is loaded twice because this was required for reliable initialization of the tested cable.

#### JTAG frequency

The tested stable programming frequency is:

```text
750000 Hz
```

Programming at **1.5 MHz was not reliable** during testing, so the application uses **750 kHz** by default.

Typical programming time:

```text
~10–11 seconds
```

Firmware format:

```text
.JED
```

---

## Fast XC9572XL Programming

The Xilinx backend uses a modified XC9500XL programming implementation in openFPGALoader.

The programming flow was optimized by comparing openFPGALoader with a working Xilinx ISE-generated SVF.

The optimized sequence follows the Xilinx SVF flow more closely:

```text
ISC_PROGRAM
    ↓
15 JEDEC data sections
    ↓
RUNTEST 20000 TCK
    ↓
Read programming status
    ↓
Next sector
```

This reduced programming time from approximately:

```text
~20 seconds
```

to approximately:

```text
~10–11 seconds
```

using the Xilinx Platform Cable USB.

---

## Erase CPLD

When using the Xilinx Platform Cable USB, the application provides an **Erase CPLD** button.

This uses the custom openFPGALoader option:

```text
--erase-only
```

Example:

```text
openFPGALoader.exe -c xilinxPlatformCableUsb --vid 0x03fd --pid 0x0008 --freq 750000 --erase-only -v
```

On the tested OpenXenium hardware:

```text
Erased CPLD     = white LED
Programmed CPLD = red LED
```

---

## Requirements

### Operating system

Tested on:

```text
Windows 11
```

The application targets:

```text
.NET 8
Windows x64
```

### USB driver

The Xilinx Platform Cable USB should use **WinUSB**.

WinUSB can be installed using a utility such as **Zadig**.

Install WinUSB for both:

```text
03FD:000F
03FD:0008
```

This avoids the old Xilinx/Jungo `windrvr6` driver and allows modern Windows security features such as **Memory Integrity** and the Microsoft vulnerable-driver blocklist to remain enabled.

---

## Tools Directory

The application expects the external flashing tools inside:

```text
Tools\
```

Example:

```text
OpenXenium CPLD Flasher\
│
├── OpenXeniumCPLDFlasher.exe
│
└── Tools\
    ├── xsvftool.exe
    ├── openFPGALoader.exe
    ├── fxload.exe
    ├── xusb_xlp_bootstrap_extracted.hex
    ├── xusb_xlp.hex
    └── required DLL files
```

### xFlasher files

Required:

```text
xsvftool.exe
```

plus any DLLs required by the xsvftool build.

### Xilinx Platform Cable USB files

Required:

```text
openFPGALoader.exe
fxload.exe
xusb_xlp_bootstrap_extracted.hex
xusb_xlp.hex
```

plus the runtime DLLs required by openFPGALoader and fxload.

---

## Xilinx Firmware Files

The Xilinx USB firmware files are **not included in the repository**.

Required files:

```text
xusb_xlp_bootstrap_extracted.hex
xusb_xlp.hex
```

`xusb_xlp.hex` originates from the Xilinx Platform Cable USB firmware supplied with Xilinx development software.

The bootstrap file used during development was extracted from the corresponding Xilinx cable driver.

Users are responsible for obtaining these files from their own legally installed Xilinx software.

---

## Usage

### xFlasher 360

1. Connect the xFlasher 360.
2. Connect JTAG to the OpenXenium.
3. Start OpenXenium CPLD Flasher.
4. Select **xFlasher 360**.
5. Click **Detect**.
6. Select the OpenXenium `.SVF` file.
7. Click **Program SVF**.

### Xilinx Platform Cable USB

1. Connect the Xilinx Platform Cable USB.
2. Connect JTAG to the OpenXenium.
3. Start OpenXenium CPLD Flasher.
4. Select **Xilinx Platform Cable USB**.
5. Click **Detect**.
6. If necessary, the software initializes the USB cable automatically.
7. Select the OpenXenium `.JED` file.
8. Click **Program JED**.

To erase the CPLD without programming it, click **Erase CPLD**.

---

## Building

Open the project in Visual Studio with the **.NET desktop development** workload installed.

Command-line build:

```text
dotnet build -c Release
```

### Single-file Windows build

```text
dotnet publish -c Release -r win-x64 --self-contained true /p:PublishSingleFile=true /p:EnableCompressionInSingleFile=true
```

Copy the required `Tools` directory next to the published executable.

---

## openFPGALoader Modifications

The Xilinx Platform Cable USB backend requires a modified openFPGALoader build.

The tested version includes changes for:

- Direct access to Xilinx Platform Cable USB `03FD:0008`
- Xilinx USB configuration 3
- Interface 0 / alternate setting 0
- WinUSB/libusb operation on Windows
- XC9572XL programming
- `--erase-only`
- Faster XC9500XL programming flow
- Stable operation at 750 kHz

An unmodified upstream openFPGALoader build may not provide all functionality required by this application.

---

## Notes

### Xilinx Platform Cable USB device state

After unplugging and reconnecting the cable it normally starts as:

```text
03FD:000F
```

The application initializes it automatically.

If the cable has already been initialized during the current USB session, it may already appear as:

```text
03FD:0008
```

### USB device numbering

`fxload` may list the Xilinx cable under a different numeric device index depending on the connected USB devices.

The application automatically selects the device matching:

```text
03FD:000F
```

or:

```text
03FD:0008
```

so the device-list position does not need to remain constant.

---

## Disclaimer

Programming CPLDs and modifying Xbox hardware carries a risk of hardware or firmware damage if incorrect files, wiring, or devices are used.

Verify the JTAG wiring and firmware before programming.

This project is provided without warranty. Use it at your own risk.

---

## Credits

This project makes use of and builds upon work from:

- **openFPGALoader**
- **xsvftool**
- **libusb**
- **fxload**
- Xilinx XC9500XL / Platform Cable USB documentation and tooling
- The OpenXenium community

Thanks to the developers and communities that made these tools and hardware projects possible.

---

## License

Add the license used by this repository here.

If third-party binaries or modified source code are distributed with the project, preserve their original licenses and copyright notices.
