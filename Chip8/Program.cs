using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.Common;
using OpenTK.Graphics.OpenGL4;
using OpenTK.Windowing.GraphicsLibraryFramework;
using OpenTK.Mathematics;
using Chip8.Core;

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
