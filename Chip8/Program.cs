
using System.Reflection.Emit;

CPU cpu = new CPU();

using (BinaryReader reader = new BinaryReader(new FileStream("ibm_logo.ch8", FileMode.Open)))
{
    while (reader.BaseStream.Position < reader.BaseStream.Length)
    {
        var opCode = (ushort)(reader.ReadByte() << 8 |  reader.ReadByte());
        // Console.WriteLine($"{opCode:X4}");

        try
        {
            cpu.ExecuteOpCode(opCode);
        }
        catch (Exception e)
        {
            Console.WriteLine(e.Message);
        }
    }
    Console.ReadKey();
}


public class CPU
{
    public byte[] RAM = new byte[4096];
    public byte[] V = new byte[16]; //Registers
    public ushort I = 0; //address
    // public ushort[] Stack = new ushort[24];
    public  Stack<ushort> Stack = new Stack<ushort>();
    public byte DelayTimer ; 
    public byte SoundTimer ; 
    public byte Keyboard ; 
    public byte[] Display = new byte[64*32];


    public void ExecuteOpCode(ushort opcode)
    {
        ushort nibble = (ushort)(opcode & 0xF000); 
        switch (nibble)
        {
            case 0x0000:
                if (opcode == 0x00e0)
                {
                    for (int i = 0; i < Display.Length; i++) Display[0] = 0;
                }
                else if (opcode == 0x00ee)
                {
                    I = Stack.Pop(); 
                }
                else
                {
                    throw new Exception($"Unknown opcode {opcode:X}");
                }
                break;
            
            case 0x1000:
                I = (ushort)(opcode & 0x0fff);
                break;
            
            case 0x2000:
                Stack.Push(I);
                I = (ushort)(opcode & 0x0FFF);
                break;
            
            case 0x3000:
                if (V[(opcode & 0x0f00) >> 8] == (opcode & 0x00ff)) I += 2;
                break;
            
            case 0x4000:
                if (V[(opcode & 0x0f00) >> 8] != (opcode & 0x00ff)) I += 2;
                break;
            
            case 0x5000:
                if (V[(opcode & 0x0f00) >> 8] == V[(opcode & 0x00f0) >> 4]) I += 2;
                break;
            
            case 0x6000:
                V[(opcode & 0x0f00) >> 8] = (byte)(opcode & 0x00FF) ;
                break;
            
            case 0x7000:
                V[(opcode & 0x0f00) >> 8] += (byte)(opcode & 0x00FF) ;
                break;
            
            case 0x8000:
                int vx = (opcode & 0x0f00) >> 8;
                int vy = (opcode & 0x00f0) >> 4;
                switch (opcode)
                {
                    case 0: V[vx] = V[vy]; break;
                    case 1: V[vx] = (byte)(V[vx] | V[vy]); break;
                    case 2: V[vx] = (byte)(V[vx] & V[vy]); break;
                    case 3: V[vx] = (byte)(V[vx] ^ V[vy]); break;
                    case 4: 
                        V[15] = (byte)(V[vx] + V[vy] > 255 ?1:0);
                        V[vx] = (byte)((V[vx] + V[vy]) & 0x00ff);
                        break; 
                    case 5: 
                        V[15] = (byte)(V[vx] > V[vy] ?1:0);
                        V[vx] = (byte)((V[vx] - V[vy]) & 0x00ff);
                        break;
                    case 6: 
                        V[15] = (byte)(V[vx] & 0x0001);
                        V[vx] = (byte)(V[vx] >>1);
                        break;
                    case 7: 
                        V[15] = (byte)(V[vy] > V[vx] ?1:0);
                        V[vx] = (byte)((V[vy] - V[vx]) & 0x00ff);
                        break;
                    case 14: 
                        V[15] = (byte)(((V[vx] & 0x80) == 0x80) ?1:0);
                        V[vx] = (byte)(V[vx] <<1);
                        break;
                    default: throw new Exception($"Unknown opcode {opcode:X}");
                }
                break;
            
                default: throw new Exception($"Unknown opcode {opcode:X}");
                
        }
    }
}