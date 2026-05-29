# CHIP-8 Emulator

A CHIP-8 emulator written in C# using OpenTK for OpenGL-accelerated rendering.

## What is CHIP-8?

CHIP-8 is an interpreted programming language developed in the 1970s for early microcomputers. It was designed to make game development simpler. Today, writing a CHIP-8 emulator is considered the classic "Hello, World!" of emulator development.

## Features

- Full CHIP-8 instruction set (35 opcodes)
- OpenGL rendering via OpenTK — runs in a proper window, not the console
- Amber/green phosphor shader for that retro CRT look
- 64×32 display scaled up 10× (640×320 window)
- Delay & sound timer support (60 Hz)
- Full hex keypad input mapped to keyboard
- Built-in font sprites (0–F)

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- A GPU that supports OpenGL 3.3+

## Running

```bash
dotnet run
```

By default it loads `heart_monitor.ch8`. To load a different ROM, change the filename in `Program.cs`:

```csharp
new FileStream("your_rom.ch8", FileMode.Open)
```

## Keyboard Layout

CHIP-8 uses a 16-key hex keypad. It is mapped to your keyboard as follows:

| CHIP-8 | Key |   | CHIP-8 | Key |
|--------|-----|---|--------|-----|
| `1`    | 1   |   | `2`    | 2   |
| `3`    | 3   |   | `C`    | 4   |
| `4`    | Q   |   | `5`    | W   |
| `6`    | E   |   | `D`    | R   |
| `7`    | A   |   | `8`    | S   |
| `9`    | D   |   | `E`    | F   |
| `A`    | Z   |   | `0`    | X   |
| `B`    | C   |   | `F`    | V   |

Press **Escape** to quit.

## Project Structure

```
Chip8/
├── Program.cs        # CPU emulator + OpenGL window (all-in-one)
├── heart_monitor.ch8 # Sample ROM — animated heart monitor waveform
├── ibm_logo.ch8      # Sample ROM — static IBM logo
└── Chip8.csproj      # Project file (uses OpenTK 4.9.3)
```

## How It Works

1. **CPU** — fetches, decodes and executes one CHIP-8 opcode at a time from 4KB of RAM
2. **Display** — a 64×32 pixel framebuffer uploaded each frame as an OpenGL texture
3. **Timers** — delay and sound timers decrement at 60 Hz in the game loop
4. **Input** — keyboard state is polled each frame and packed into a 16-bit bitmask

## References

- [Cowgod's CHIP-8 Technical Reference](http://devernay.free.fr/hacks/chip8/C8TECH10.HTM)
- [Guide to making a CHIP-8 emulator — Tobias V. Langhoff](https://tobiasvl.github.io/blog/write-a-chip-8-emulator/)



