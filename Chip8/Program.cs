using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;

// ── Entry point ─────────────────────────────────────────────────────────────
CPU cpu = new CPU();

using (var reader = new BinaryReader(new FileStream("heart_monitor.ch8", FileMode.Open)))
{
    var program = new List<byte>();
    while (reader.BaseStream.Position < reader.BaseStream.Length)
        program.Add(reader.ReadByte());
    cpu.LoadProgram(program.ToArray());
}

var nativeSettings = new NativeWindowSettings
{
    ClientSize  = new Vector2i(64 * 10, 32 * 10),
    Title       = "CHIP-8 Emulator",
    APIVersion  = new Version(3, 3),
    Profile     = ContextProfile.Core,
};

using var window = new Chip8Window(GameWindowSettings.Default, nativeSettings, cpu);
window.Run();

// ── OpenGL Window ────────────────────────────────────────────────────────────
public class Chip8Window : GameWindow
{
    private readonly CPU _cpu;
    private int _vao, _vbo, _shader, _texture;

    // CHIP-8 key map: CHIP-8 key index → GLFW Keys
    private static readonly Keys[] KeyMap = new Keys[16]
    {
        Keys.X,    // 0
        Keys.D1,   // 1
        Keys.D2,   // 2
        Keys.D3,   // 3
        Keys.Q,    // 4
        Keys.W,    // 5
        Keys.E,    // 6
        Keys.A,    // 7
        Keys.S,    // 8
        Keys.D,    // 9
        Keys.Z,    // A
        Keys.C,    // B
        Keys.D4,   // C
        Keys.R,    // D
        Keys.F,    // E
        Keys.V,    // F
    };

    public Chip8Window(GameWindowSettings gws, NativeWindowSettings nws, CPU cpu)
        : base(gws, nws)
    {
        _cpu = cpu;
    }

    protected override void OnLoad()
    {
        base.OnLoad();
        GL.ClearColor(0f, 0f, 0f, 1f);

        // Full-screen quad
        float[] vertices = {
            // x      y      u     v
            -1f,  1f,   0f,  0f,
             1f,  1f,   1f,  0f,
             1f, -1f,   1f,  1f,
            -1f,  1f,   0f,  0f,
             1f, -1f,   1f,  1f,
            -1f, -1f,   0f,  1f,
        };

        _vao = GL.GenVertexArray();
        _vbo = GL.GenBuffer();
        GL.BindVertexArray(_vao);
        GL.BindBuffer(BufferTarget.ArrayBuffer, _vbo);
        GL.BufferData(BufferTarget.ArrayBuffer, vertices.Length * sizeof(float), vertices, BufferUsageHint.StaticDraw);
        GL.VertexAttribPointer(0, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 0);
        GL.EnableVertexAttribArray(0);
        GL.VertexAttribPointer(1, 2, VertexAttribPointerType.Float, false, 4 * sizeof(float), 2 * sizeof(float));
        GL.EnableVertexAttribArray(1);

        // Shader
        const string vert = @"#version 330 core
layout(location=0) in vec2 aPos;
layout(location=1) in vec2 aUV;
out vec2 uv;
void main(){ gl_Position = vec4(aPos,0,1); uv = aUV; }";

        const string frag = @"#version 330 core
in vec2 uv;
out vec4 color;
uniform sampler2D tex;
void main(){
    float v = texture(tex, uv).r;
    color = vec4(v, v*0.9, 0.0, 1.0);  // amber/green phosphor look
}";
        int vs = GL.CreateShader(ShaderType.VertexShader);
        GL.ShaderSource(vs, vert);
        GL.CompileShader(vs);

        int fs = GL.CreateShader(ShaderType.FragmentShader);
        GL.ShaderSource(fs, frag);
        GL.CompileShader(fs);

        _shader = GL.CreateProgram();
        GL.AttachShader(_shader, vs);
        GL.AttachShader(_shader, fs);
        GL.LinkProgram(_shader);
        GL.DeleteShader(vs);
        GL.DeleteShader(fs);

        // Texture: 64×32 single-channel
        _texture = GL.GenTexture();
        GL.BindTexture(TextureTarget.Texture2D, _texture);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Nearest);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Nearest);
        GL.TexImage2D(TextureTarget.Texture2D, 0, PixelInternalFormat.R8, 64, 32, 0,
                      PixelFormat.Red, PixelType.UnsignedByte, (nint)0);
    }

    protected override void OnUpdateFrame(FrameEventArgs args)
    {
        base.OnUpdateFrame(args);

        // Build keyboard state (16 keys)
        ushort keys = 0;
        for (int i = 0; i < 16; i++)
            if (KeyboardState.IsKeyDown(KeyMap[i]))
                keys |= (ushort)(1 << i);
        _cpu.Keys = keys;

        // Run ~10 CPU steps per frame
        for (int i = 0; i < 10; i++)
            _cpu.Step();

        // Decrement timers at 60 Hz
        if (_cpu.DelayTimer > 0) _cpu.DelayTimer--;
        if (_cpu.SoundTimer > 0) _cpu.SoundTimer--;

        if (KeyboardState.IsKeyDown(Keys.Escape)) Close();
    }

    protected override void OnRenderFrame(FrameEventArgs args)
    {
        base.OnRenderFrame(args);
        GL.Clear(ClearBufferMask.ColorBufferBit);

        // Upload display to texture
        GL.BindTexture(TextureTarget.Texture2D, _texture);
        byte[] pixels = new byte[64 * 32];
        for (int i = 0; i < pixels.Length; i++)
            pixels[i] = (byte)(_cpu.Display[i] != 0 ? 255 : 0);
        GL.TexSubImage2D(TextureTarget.Texture2D, 0, 0, 0, 64, 32,
                         PixelFormat.Red, PixelType.UnsignedByte, pixels);

        GL.UseProgram(_shader);
        GL.BindVertexArray(_vao);
        GL.DrawArrays(PrimitiveType.Triangles, 0, 6);

        SwapBuffers();
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUnload()
    {
        base.OnUnload();
        GL.DeleteVertexArray(_vao);
        GL.DeleteBuffer(_vbo);
        GL.DeleteProgram(_shader);
        GL.DeleteTexture(_texture);
    }
}

// ── CPU ───────────────────────────────────────────────────────────────────────
public class CPU
{
    public byte[] RAM     = new byte[4096];
    public byte[] V       = new byte[16];
    public ushort PC      = 0;
    public ushort I       = 0;
    public Stack<ushort> Stack = new Stack<ushort>();
    public byte   DelayTimer;
    public byte   SoundTimer;
    public ushort Keys;                      // bitmask, bit N = CHIP-8 key N
    public byte[] Display = new byte[64 * 32];

    private bool   _waitingForKey = false;
    private int    _waitReg       = 0;
    private Random _rng           = new Random();

    // Built-in font (0–F, each 5 bytes)
    private static readonly byte[] Font = {
        0xF0,0x90,0x90,0x90,0xF0, // 0
        0x20,0x60,0x20,0x20,0x70, // 1
        0xF0,0x10,0xF0,0x80,0xF0, // 2
        0xF0,0x10,0xF0,0x10,0xF0, // 3
        0x90,0x90,0xF0,0x10,0x10, // 4
        0xF0,0x80,0xF0,0x10,0xF0, // 5
        0xF0,0x80,0xF0,0x90,0xF0, // 6
        0xF0,0x10,0x20,0x40,0x40, // 7
        0xF0,0x90,0xF0,0x90,0xF0, // 8
        0xF0,0x90,0xF0,0x10,0xF0, // 9
        0xF0,0x90,0xF0,0x90,0x90, // A
        0xE0,0x90,0xE0,0x90,0xE0, // B
        0xF0,0x80,0x80,0x80,0xF0, // C
        0xE0,0x90,0x90,0x90,0xE0, // D
        0xF0,0x80,0xF0,0x80,0xF0, // E
        0xF0,0x80,0xF0,0x80,0x80, // F
    };

    public void LoadProgram(byte[] program)
    {
        // Load font at 0x050
        for (int i = 0; i < Font.Length; i++)
            RAM[0x050 + i] = Font[i];

        for (int i = 0; i < program.Length; i++)
            RAM[0x200 + i] = program[i];
        PC = 0x200;
    }

    public void Step()
    {
        // Handle FX0A wait
        if (_waitingForKey)
        {
            for (int k = 0; k < 16; k++)
            {
                if (((Keys >> k) & 1) == 1)
                {
                    V[_waitReg] = (byte)k;
                    _waitingForKey = false;
                    break;
                }
            }
            return;
        }

        ushort op = (ushort)(RAM[PC] << 8 | RAM[PC + 1]);
        PC += 2;

        int x  = (op & 0x0F00) >> 8;
        int y  = (op & 0x00F0) >> 4;
        int n  =  op & 0x000F;
        int nn =  op & 0x00FF;
        int nnn= op & 0x0FFF;

        switch (op & 0xF000)
        {
            case 0x0000:
                if (op == 0x00E0) Array.Clear(Display, 0, Display.Length);
                else if (op == 0x00EE) PC = Stack.Pop();
                break;
            case 0x1000: PC = (ushort)nnn; break;
            case 0x2000: Stack.Push(PC); PC = (ushort)nnn; break;
            case 0x3000: if (V[x] == nn) PC += 2; break;
            case 0x4000: if (V[x] != nn) PC += 2; break;
            case 0x5000: if (V[x] == V[y]) PC += 2; break;
            case 0x6000: V[x] = (byte)nn; break;
            case 0x7000: V[x] = (byte)(V[x] + nn); break;
            case 0x8000:
                switch (n)
                {
                    case 0: V[x] = V[y]; break;
                    case 1: V[x] |= V[y]; break;
                    case 2: V[x] &= V[y]; break;
                    case 3: V[x] ^= V[y]; break;
                    case 4:
                        V[0xF] = (byte)(V[x] + V[y] > 255 ? 1 : 0);
                        V[x]   = (byte)(V[x] + V[y]);
                        break;
                    case 5:
                        V[0xF] = (byte)(V[x] >= V[y] ? 1 : 0);
                        V[x]   = (byte)(V[x] - V[y]);
                        break;
                    case 6:
                        V[0xF] = (byte)(V[x] & 1);
                        V[x]   >>= 1;
                        break;
                    case 7:
                        V[0xF] = (byte)(V[y] >= V[x] ? 1 : 0);
                        V[x]   = (byte)(V[y] - V[x]);
                        break;
                    case 0xE:
                        V[0xF] = (byte)((V[x] >> 7) & 1);
                        V[x]   <<= 1;
                        break;
                }
                break;
            case 0x9000: if (V[x] != V[y]) PC += 2; break;
            case 0xA000: I = (ushort)nnn; break;
            case 0xB000: PC = (ushort)(nnn + V[0]); break;
            case 0xC000: V[x] = (byte)(_rng.Next(256) & nn); break;
            case 0xD000:
            {
                int px = V[x] % 64, py = V[y] % 32;
                V[0xF] = 0;
                for (int row = 0; row < n; row++)
                {
                    if (py + row >= 32) break;
                    byte b = RAM[I + row];
                    for (int col = 0; col < 8; col++)
                    {
                        if (px + col >= 64) break;
                        if (((b >> (7 - col)) & 1) == 1)
                        {
                            int idx = (px + col) + (py + row) * 64;
                            if (Display[idx] == 1) V[0xF] = 1;
                            Display[idx] ^= 1;
                        }
                    }
                }
                break;
            }
            case 0xE000:
                bool pressed = ((Keys >> V[x]) & 1) == 1;
                if (nn == 0x9E && pressed)  PC += 2;
                if (nn == 0xA1 && !pressed) PC += 2;
                break;
            case 0xF000:
                switch (nn)
                {
                    case 0x07: V[x] = DelayTimer; break;
                    case 0x0A: _waitingForKey = true; _waitReg = x; break;
                    case 0x15: DelayTimer = V[x]; break;
                    case 0x18: SoundTimer = V[x]; break;
                    case 0x1E: I = (ushort)(I + V[x]); break;
                    case 0x29: I = (ushort)(0x050 + V[x] * 5); break;
                    case 0x33:
                        RAM[I]   = (byte)(V[x] / 100);
                        RAM[I+1] = (byte)(V[x] / 10 % 10);
                        RAM[I+2] = (byte)(V[x] % 10);
                        break;
                    case 0x55: for (int i = 0; i <= x; i++) RAM[I + i] = V[i]; break;
                    case 0x65: for (int i = 0; i <= x; i++) V[i] = RAM[I + i]; break;
                }
                break;
        }
    }
}