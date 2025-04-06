class Program
{
    static void Main(string[] args)
    {
        if (args.Length > 0)
        {
            bool debug = false;
            string fileName = string.Empty;

            for (int i = 0; i < args.Length; i++)
            {
                switch(args[i].ToLower())
                {
                    // Enable debug
                    case ("-d"):
                        debug = true;
                        break;

                    // File
                    case ("-f"):
                        if ((i + 1) >= args.Length)
                        {
                            Console.WriteLine("Error: Missing filename after -f flag.");
                            Environment.Exit(0);
                        }

                        fileName = args[i + 1];

                        i++;

                        break;
                }
            }

            // Requires a file
            if (String.IsNullOrEmpty(fileName))
            {
                Console.WriteLine("Error: Missing file, use the -f flag to input a file.");
                Environment.Exit(0);
            }

            // Create chip8
            Chip8 chip8 = new Chip8(fileName, debug);
            //chip8.WriteMemory();

            while(true)
            {
                chip8.RunChip8();
            }
        }
    }
}

public struct Registers()
{
    // Data Registers range from V0 to VF
    // 16 in total
    public byte v0 = 0;
    public byte v1 = 0;
    public byte v2 = 0;
    public byte v3 = 0;
    public byte v4 = 0;
    public byte v5 = 0;
    public byte v6 = 0;
    public byte v7 = 0;
    public byte v8 = 0;
    public byte v9 = 0;
    public byte vA = 0;
    public byte vB = 0;
    public byte vC = 0;
    public byte vD = 0;
    public byte vE = 0;
    public byte vF = 0;

    // Address register
    // Can only be loaded with a 12 bit memory address
    public UInt16 iReg = 0x000;
}

public class Chip8
{
    // Sets debug mode
    public bool debug { get; private set; } = false;

    // 4 Kilobytes of ram
    public byte[] memory { get; private set; } = new byte[4096];

    // Black and white
    // Screen width is 64 height is 32
    public bool[,] display { get; private set; } = new bool[64, 32];

    // Program counter can only access 12 bits
    // Programs start at 0x200
    public UInt16 pc { get; private set; } = 0x200;

    // Create the stack to store 16 bit addresses
    public Stack<UInt16> stack { get; private set; } = new Stack<UInt16>();

    // Delay timer decrements by 1 60 times per second
    public byte delayTimer { get; private set; } = 0;

    // Sound timer decrements by 1 60 times per second
    // Plays a beep if greater than 1
    public byte soundTimer { get; private set; } = 0;

    // Registers for the chip 8
    public Registers registers { get; private set; } = new Registers();

    // Chip 8 font
    private byte[] font =
    [
        0xF0, 0x90, 0x90, 0x90, 0xF0, // 0
        0x20, 0x60, 0x20, 0x20, 0x70, // 1
        0xF0, 0x10, 0xF0, 0x80, 0xF0, // 2
        0xF0, 0x10, 0xF0, 0x10, 0xF0, // 3
        0x90, 0x90, 0xF0, 0x10, 0x10, // 4
        0xF0, 0x80, 0xF0, 0x10, 0xF0, // 5
        0xF0, 0x80, 0xF0, 0x90, 0xF0, // 6
        0xF0, 0x10, 0x20, 0x40, 0x40, // 7
        0xF0, 0x90, 0xF0, 0x90, 0xF0, // 8
        0xF0, 0x90, 0xF0, 0x10, 0xF0, // 9
        0xF0, 0x90, 0xF0, 0x90, 0x90, // A
        0xE0, 0x90, 0xE0, 0x90, 0xE0, // B
        0xF0, 0x80, 0x80, 0x80, 0xF0, // C
        0xE0, 0x90, 0x90, 0x90, 0xE0, // D
        0xF0, 0x80, 0xF0, 0x80, 0xF0, // E
        0xF0, 0x80, 0xF0, 0x80, 0x80  // F
    ];
    
    private void LoadFile(string fileName)
    {
        if (debug) Console.WriteLine($"CHIP8: Loading {fileName} into Memory");

        // Load in memory starting at 0x200
        byte[] file = [];
        try
        {
            file = File.ReadAllBytes(fileName);
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error: {ex.Message}");
            Environment.Exit(1);
        }

        Array.Copy(file, 0, memory, 0x200, file.Length);
    }

    private void LoadFont()
    {
        if (debug) Console.WriteLine("CHIP8: Loading Font into Memory.");

        // Write the font to memory addresses starting at 0x00
        Array.Copy(font, 0, memory, 0x00, font.Length);
    }

    public void WriteMemory()
    {
        Console.Write("\n\nMEMORY:\n\n");

        for (UInt16 i = 0; i < memory.Length; i++)
        {
            Console.Write($"{i.ToString("X3")}:\t");
            Console.Write($"{memory[i].ToString("X2")}\n");
        }
    }

    public UInt16 FetchOpcode(UInt16 address)
    {
        if (address >= memory.Length)
        {
            Console.WriteLine("CHIP8: Reached the end of memory, exiting program.");
            Environment.Exit(0);
        }

        // Get instruction
        byte highByte = memory[address];

        if ((address + 1) >= memory.Length)
        {
            Console.WriteLine("Error: Program Counter reached an out of bounds range.");
            Environment.Exit(1);
        }

        byte lowByte = memory[address + 1];

        // Create the opcode.
        UInt16 opcode = (UInt16)((highByte << 8) | lowByte);

        return opcode;
    }

    public void RunChip8()
    {
        UInt16 opcode = FetchOpcode(pc);
        // Increment Program Counter
        pc += 0x2;

        DecodeInstruction(opcode);
    }

    public void DecodeInstruction(UInt16 opcode)
    {
        // Read only the first nibble
        switch ((opcode >> 12) & 0xF)
        {
            case 0x0:
                switch(opcode & 0xFFF)
                {
                    case 0x0E0:
                        // Clear Screen
                        Execute00E0();
                        break;
                    case 0x0EE:
                        // Return from a subroutine
                        Execute00EE();
                        break;
                    default:
                        // Execute instruction at NNN
                        Execute0NNN((UInt16)(opcode & 0xFFF));
                        break;
                }
                break;

            case 0x1:
                // Jump to NNN
                break;
            case 0x2:
                // Execute subroutine at NNN
                break;
            case 0x3:
                // Skip following instruction if VX == NN
                break;
            case 0x4:
                // Skip following instruction if VX != NN
                break;
            case 0x5:
                // Skip following instruction if VX == VY
                break;
            case 0x6:
                // Store NN in VX
                break;
            case 0x7:
                // Add NN to VX
                break;
            case 0x8:
                Console.WriteLine("8");
                break;
            case 0x9:
                Console.WriteLine("9");
                break;
            case 0xA:
                Console.WriteLine("A");
                break;
            case 0xB:
                Console.WriteLine("B");
                break;
            case 0xC:
                Console.WriteLine("C");
                break;
            case 0xD:
                Console.WriteLine("D");
                break;
            case 0xE:
                Console.WriteLine("E");
                break;
            case 0xF:
                Console.WriteLine("F");
                break;
        }
    }

    public void Execute0NNN(UInt16 address)
    {
        if (debug) Console.WriteLine($"CHIP8: Executing 0NNN, Running Opcode at 0x0{address.ToString("X3")}.");

        // Make sure address is within bounds
        address = (UInt16)(address & 0xFFF);

        // Get opcode from memory
        UInt16 opcode = FetchOpcode(address);

        // Run opcode
        DecodeInstruction(opcode);
    }

    public void Execute00E0()
    {
        if (debug) Console.WriteLine("CHIP8: Executing 00E0, Clearing Screen.");

        // Clear the screen
        for (int i = 0; i < display.GetLength(0); i++)
        {
            for (int j = 0; j < display.GetLength(1); j++)
            {
                display[i, j] = false;
            }
        }
    }

    public void Execute00EE()
    {
        // Return from a subroutine
        Console.WriteLine("OPCODE NOT IMPLEMENTED");
    }

    public Chip8(string fileName)
    {
        LoadFont();
        LoadFile(fileName);
    }

    public Chip8(string fileName, bool _debug)
    {
        debug = _debug;

        LoadFont();
        LoadFile(fileName);
    }
}