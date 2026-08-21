# CHIP-8 Emulator

A CHIP-8 emulator written in C# — runs natively with OpenGL, and **in your browser** via Blazor WebAssembly.

**▶ [Try it live](https://youssefhikal93.github.io/CHIP-8-emulator/)** — pick a ROM (IBM Logo or Heart Monitor) and it runs right in the browser, no install needed.

## What is CHIP-8?

CHIP-8 is an interpreted programming language developed in the 1970s for early microcomputers. It was designed to make game development simpler. Today, writing a CHIP-8 emulator is considered the classic "Hello, World!" of emulator development.

## Features

- Full CHIP-8 instruction set (35 opcodes)
- One shared CPU core (`Chip8.Core`), two front-ends:
  - **Desktop** — OpenGL rendering via OpenTK with an amber phosphor shader
  - **Web** — the same C# CPU compiled to WebAssembly, rendered to a `<canvas>` with CRT scanlines, deployed on GitHub Pages
- ROM selector in the web version (IBM Logo / Heart Monitor)
- Delay & sound timer support (60 Hz), beeper via WebAudio in the browser
- Full hex keypad input mapped to keyboard
- Built-in font sprites (0–F)

## Requirements

- [.NET 9 SDK](https://dotnet.microsoft.com/download)
- Desktop version: a GPU that supports OpenGL 3.3+

## Running

Desktop version:

```bash
dotnet run --project Chip8
```

Web version (then open http://localhost:5188):

```bash
dotnet run --project Chip8.Web --urls http://localhost:5188
```

The desktop version loads `heart_monitor.ch8` by default. To load a different ROM, change the filename in `Chip8/Program.cs`.

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

Press **Escape** to quit the desktop version.

## Project Structure

```
├── Chip8.Core/           # Shared emulator core
│   └── CPU.cs            # Fetch/decode/execute, RAM, registers, timers, display buffer
├── Chip8/                # Desktop front-end (OpenTK / OpenGL)
│   ├── Program.cs        # Window, shader, input polling
│   ├── heart_monitor.ch8 # Sample ROM — animated heart monitor waveform
│   └── ibm_logo.ch8      # Sample ROM — static IBM logo
├── Chip8.Web/            # Web front-end (Blazor WebAssembly)
│   ├── App.razor         # Emulator component: ROM selector + JS interop tick
│   └── wwwroot/
│       ├── js/chip8.js   # requestAnimationFrame loop, canvas drawing, keyboard, beeper
│       └── roms/         # ROMs served as static files
└── .github/workflows/    # Auto-deploy of the web version to GitHub Pages
```

## How It Works

1. **CPU** — fetches, decodes and executes one CHIP-8 opcode at a time from 4KB of RAM
2. **Display** — a 64×32 pixel framebuffer; the desktop uploads it as an OpenGL texture, the web version draws it into an `ImageData` and scales it onto a canvas with pixelated rendering
3. **Timers** — delay and sound timers decrement at 60 Hz in the game loop
4. **Input** — keyboard state is packed into a 16-bit bitmask (polled per frame on desktop, event-driven in the browser)

## Deployment

Every push to `main` builds the Blazor WebAssembly app and publishes it to GitHub Pages via [deploy-pages.yml](.github/workflows/deploy-pages.yml) — free static hosting, everything runs client-side.

## References

- [Cowgod's CHIP-8 Technical Reference](http://devernay.free.fr/hacks/chip8/C8TECH10.HTM)
- [Guide to making a CHIP-8 emulator — Tobias V. Langhoff](https://tobiasvl.github.io/blog/write-a-chip-8-emulator/)
