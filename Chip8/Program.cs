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
    public byte[] v = new byte[16]; // V0 to VF

    // Address register
    // Can only be loaded with a 12 bit memory address
    public UInt16 i = 0x000;
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
                Execute1NNN((UInt16)(opcode & 0xFFF));
                break;

            case 0x2:
                // Execute subroutine at NNN
                Execute2NNN((UInt16)(opcode & 0xFFF));
                break;

            case 0x3:
                // Skip following instruction if VX == NN
                Execute3XNN((byte)((opcode >> 8) & 0xF), (byte)(opcode & 0xFF));
                break;

            case 0x4:
                // Skip following instruction if VX != NN
                Execute4XNN((byte)((opcode >> 8) & 0xF), (byte)(opcode & 0xFF));
                break;

            case 0x5:
                switch (opcode & 0xF)
                {
                    // 5XY0
                    case 0x0:
                        // Skip following instruction if VX == VY
                        Execute5XY0((byte)((opcode >> 8) & 0xF), (byte)((opcode >> 4) & 0xF));
                        break;
                    default:
                        // Invalid opcode
                        Errors.ErrorInvalidOpcode(opcode);
                        break;
                }
                break;

            case 0x6:
                // Store NN in VX
                Execute6XNN((byte)((opcode >> 8) & 0xF), (byte)(opcode & 0xFF));
                break;

            case 0x7:
                // Add NN to VX
                Execute7XNN((byte)((opcode >> 8) & 0xF), (byte)(opcode & 0xFF));
                break;

            case 0x8:
                switch (opcode & 0xF)
                {
                    case 0x0:
                        // Set VX to VY
                        Execute8XY0((byte)((opcode >> 8) & 0xF), (byte)((opcode >> 4) & 0xF));
                        break;
                    case 0x1:
                        // Set VX to bitwise OR of VX and VY
                        // Quirk Reset VF
                        Execute8XY1((byte)((opcode >> 8) & 0xF), (byte)((opcode >> 4) & 0xF));
                        break;
                    case 0x2:
                        // Set VX to bitwise AND of VX and VY
                        // Quirk Reset VF
                        Execute8XY2((byte)((opcode >> 8) & 0xF), (byte)((opcode >> 4) & 0xF));
                        break;
                    case 0x3:
                        // Set VX to bitwise XOR of VX and VY
                        // Quirk Reset VF
                        Execute8XY3((byte)((opcode >> 8) & 0xF), (byte)((opcode >> 4) & 0xF));
                        break;
                    case 0x4:
                        // Add VY to VX
                        // Set VF to 1 if there is a carry else 0
                        Execute8XY4((byte)((opcode >> 8) & 0xF), (byte)((opcode >> 4) & 0xF));
                        break;
                    case 0x5:
                        // Subtract VY from VX
                        // Set VF to 0 if there is a borrow else 1
                        Execute8XY5((byte)((opcode >> 8) & 0xF), (byte)((opcode >> 4) & 0xF));
                        break;
                    case 0x6:
                        // Set VX to VY and shift VX one bit to the right. Set VF to the value of the least significant bit of VX before the shift.
                        // Quirk Don't set VX to VY only shift VX
                        Execute8XY6((byte)((opcode >> 8) & 0xF), (byte)((opcode >> 4) & 0xF));
                        break;
                    case 0x7:
                        // Set VX to the result of subtracting VX from VY. VF is set to 0 if there is a borrow, and 1 if there is not.
                        // Set VF to 0 if there is a borrow else 1
                        Execute8XY7((byte)((opcode >> 8) & 0xF), (byte)((opcode >> 4) & 0xF));
                        break;
                    case 0xE:
                        // Set VX to VY and shift VX one bit to the left. Set VF to the value of the most significant bit of VX before the shift.
                        // Quirk Don't set VX to VY only shift VX
                        Execute8XYE((byte)((opcode >> 8) & 0xF), (byte)((opcode >> 4) & 0xF));
                        break;
                    default:
                        // Invalid opcode
                        Errors.ErrorInvalidOpcode(opcode);
                        break;
                }
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
        // Execute instruction at NNN
        if (debug) Console.WriteLine($"CHIP8: Executing 0NNN, Running opcode at 0x0{address.ToString("X3")}.");

        // Make sure address is within bounds
        if (address >= memory.Length)
            Errors.ErrorOutOfBounds(address);

        // Get opcode from memory
        UInt16 opcode = FetchOpcode(address);

        // Run opcode
        DecodeInstruction(opcode);
    }

    public void Execute00E0()
    {
        if (debug) Console.WriteLine("CHIP8: Executing 00E0, Clearing screen.");

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
        if (debug) Console.WriteLine("CHIP8: Executing 00EE, Returning from subroutine.");

        // Make sure stack is not empty
        if (stack.Count == 0)
            Errors.ErrorEmptyStack();

        // Pop the top address from the stack
        UInt16 address = stack.Pop();

        // Make sure address is within bounds
        if (address >= memory.Length)
            Errors.ErrorOutOfBounds(address);

        // Set program counter to address
        pc = address;
    }

    public void Execute1NNN(UInt16 address)
    {
        // Jump to NNN
        if (debug) Console.WriteLine($"CHIP8: Executing 1NNN, Jumping to 0x0{address.ToString("X3")}.");

        // Make sure address is within bounds
        if (address >= memory.Length)
            Errors.ErrorOutOfBounds(address);

        // Set program counter to address
        pc = address;
    }

    public void Execute2NNN(UInt16 address)
    {
        // Execute subroutine at NNN
        if (debug) Console.WriteLine($"CHIP8: Executing 2NNN, Executing subroutine at 0x0{address.ToString("X3")}.");

        // Make sure address is within bounds
        if (address >= memory.Length)
            Errors.ErrorOutOfBounds(address);

        // Push the current program counter to the stack
        stack.Push(pc);

        // Set program counter to address
        pc = address;
    }

    public void Execute3XNN(byte x, byte nn)
    {
        // Skip next instruction if VX == NN
        if (debug) Console.WriteLine($"CHIP8: Executing 3XNN, Skipping next instruction if V{x.ToString("X1")} == {nn.ToString("X2")}.");

        // Make sure x is within bounds
        if (x > 0xF)
            Errors.ErrorInvalidRegister(x);

        if (registers.v[x] == nn)
        {
            // Skip next instruction
            pc += 0x2;
        }
    }

    public void Execute4XNN(byte x, byte nn)
    {
        // Skip next instruction if VX != NN
        if (debug) Console.WriteLine($"CHIP8: Executing 4XNN, Skipping next instruction if V{x.ToString("X1")} != {nn.ToString("X2")}.");

        // Make sure x is within bounds
        if (x > 0xF)
            Errors.ErrorInvalidRegister(x);

        if (registers.v[x] != nn)
        {
            // Skip next instruction
            pc += 0x2;
        }
    }

    public void Execute5XY0(byte x, byte y)
    {
        // Skip next instruction if VX == VY
        if (debug) Console.WriteLine($"CHIP8: Executing 5XY0, Skipping next instruction if V{x.ToString("X1")} == V{y.ToString("X1")}.");

        // Make sure x and y are within bounds
        if (x > 0xF)
            Errors.ErrorInvalidRegister(x);

        if (y > 0xF)
            Errors.ErrorInvalidRegister(y);

        if (registers.v[x] == registers.v[y])
        {
            // Skip next instruction
            pc += 0x2;
        }
    }

    public void Execute6XNN(byte x, byte nn)
    {
        // Store NN in VX
        if (debug) Console.WriteLine($"CHIP8: Executing 6XNN, Storing {nn.ToString("X2")} in V{x.ToString("X1")}.");

        // Make sure x is within bounds
        if (x > 0xF)
            Errors.ErrorInvalidRegister(x);

        // Store nn in vx
        registers.v[x] = nn;
    }

    public void Execute7XNN(byte x, byte nn)
    {
        // Add NN to VX
        if (debug) Console.WriteLine($"CHIP8: Executing 7XNN, Adding {nn.ToString("X2")} to V{x.ToString("X1")}.");

        // Make sure x is within bounds
        if (x > 0xF)
            Errors.ErrorInvalidRegister(x);

        // Add nn to vx
        registers.v[x] += nn;
    }

    public void Execute8XY0(byte x, byte y)
    {
        // Set VX to VY
        if (debug) Console.WriteLine($"CHIP8: Executing 8XY0, Setting V{x.ToString("X1")} to V{y.ToString("X1")}.");

        // Make sure x and y are within bounds
        if (x > 0xF)
            Errors.ErrorInvalidRegister(x);

        if (y > 0xF)
            Errors.ErrorInvalidRegister(y);

        // Set vx to vy
        registers.v[x] = registers.v[y];
    }

    public void Execute8XY1(byte x, byte y)
    {
        // Set VX to bitwise OR of VX and VY
        if (debug) Console.WriteLine($"CHIP8: Executing 8XY1, Setting V{x.ToString("X1")} to V{x.ToString("X1")} | V{y.ToString("X1")}.");

        // Make sure x and y are within bounds
        if (x > 0xF)
            Errors.ErrorInvalidRegister(x);

        if (y > 0xF)
            Errors.ErrorInvalidRegister(y);

        // Set vx to vx | vy
        registers.v[x] = (byte)(registers.v[x] | registers.v[y]);

        // Quirk Reset VF
        registers.v[0xF] = 0;
    }

    public void Execute8XY2(byte x, byte y)
    {
        // Set VX to bitwise AND of VX and VY
        if (debug) Console.WriteLine($"CHIP8: Executing 8XY2, Setting V{x.ToString("X1")} to V{x.ToString("X1")} & V{y.ToString("X1")}.");

        // Make sure x and y are within bounds
        if (x > 0xF)
            Errors.ErrorInvalidRegister(x);

        if (y > 0xF)
            Errors.ErrorInvalidRegister(y);

        // Set vx to vx & vy
        registers.v[x] = (byte)(registers.v[x] & registers.v[y]);

        // Quirk Reset VF
        registers.v[0xF] = 0;
    }

    public void Execute8XY3(byte x, byte y)
    {
        // Set VX to bitwise XOR of VX and VY
        if (debug) Console.WriteLine($"CHIP8: Executing 8XY3, Setting V{x.ToString("X1")} to V{x.ToString("X1")} ^ V{y.ToString("X1")}.");

        // Make sure x and y are within bounds
        if (x > 0xF)
            Errors.ErrorInvalidRegister(x);

        if (y > 0xF)
            Errors.ErrorInvalidRegister(y);

        // Set vx to vx ^ vy
        registers.v[x] = (byte)(registers.v[x] ^ registers.v[y]);

        // Quirk Reset VF
        registers.v[0xF] = 0;
    }

    public void Execute8XY4(byte x, byte y)
    {
        if (debug) Console.WriteLine($"CHIP8: Executing 8XY4, Adding V{y.ToString("X1")} to V{x.ToString("X1")}.");

        // Make sure x and y are within bounds
        if (x > 0xF)
            Errors.ErrorInvalidRegister(x);

        if (y > 0xF)
            Errors.ErrorInvalidRegister(y);

        // Add VY to VX
        UInt16 sum = (UInt16)(registers.v[x] + registers.v[y]);

        // Set VX to the sum
        registers.v[x] = (byte)(sum & 0xFF);

        // Set VF to 1 if there is a carry else 0
        registers.v[0xF] = sum > 0xFF ? (byte)0x1 : (byte)0x0;
    }

    public void Execute8XY5(byte x, byte y)
    {
        // Subtract VY from VX
        if (debug) Console.WriteLine($"CHIP8: Executing 8XY5, Subtracting V{y.ToString("X1")} from V{x.ToString("X1")}.");

        // Make sure x and y are within bounds
        if (x > 0xF)
            Errors.ErrorInvalidRegister(x);

        if (y > 0xF)
            Errors.ErrorInvalidRegister(y);

        // Set borrow
        byte borrow = registers.v[x] > registers.v[y] ? (byte)0x1 : (byte)0x0;

        // Subtract VY from VX
        registers.v[x] -= registers.v[y];

        // Set VF to 0 if there is a borrow else 1
        registers.v[0xF] = borrow;
    }

    public void Execute8XY6(byte x, byte y)
    {
        // Set VX to VY and shift VX one bit to the right. Set VF to the value of the least significant bit of VX before the shift.
        if (debug) Console.WriteLine($"CHIP8: Executing 8XY6, Shifting V{x.ToString("X1")} one bit to the right.");

        // Make sure x and y are within bounds
        if (x > 0xF)
            Errors.ErrorInvalidRegister(x);

        if (y > 0xF)
            Errors.ErrorInvalidRegister(y);

        // Set least significant bit
        byte leastSignificant = (byte)(registers.v[x] & 0x1);

        // Shift VX one bit to the right
        registers.v[x] = (byte)(registers.v[x] >> 1);

        // Set VF to the value of the least significant bit of VX before the shift
        registers.v[0xF] = leastSignificant;
    }

    public void Execute8XY7(byte x, byte y)
    {
        // Set VX to the result of subtracting VX from VY. VF is set to 0 if there is a borrow, and 1 if there is not.
        if (debug) Console.WriteLine($"CHIP8: Executing 8XY7, Subtracting V{x.ToString("X1")} from V{y.ToString("X1")}.");

        // Make sure x and y are within bounds
        if (x > 0xF)
            Errors.ErrorInvalidRegister(x);

        if (y > 0xF)
            Errors.ErrorInvalidRegister(y);

        // Set borrow
        byte borrow = registers.v[y] > registers.v[x] ? (byte)0x1 : (byte)0x0;

        // Subtract VX from VY
        registers.v[x] = (byte)(registers.v[y] - registers.v[x]);

        // Set VF to 0 if there is a borrow else 1
        registers.v[0xF] = borrow;
    }

    public void Execute8XYE(byte x, byte y)
    {
        // Set VX to VY and shift VX one bit to the left. Set VF to the value of the most significant bit of VX before the shift.
        if (debug) Console.WriteLine($"CHIP8: Executing 8XYE, Shifting V{x.ToString("X1")} one bit to the left.");

        // Make sure x and y are within bounds
        if (x > 0xF)
            Errors.ErrorInvalidRegister(x);

        if (y > 0xF)
            Errors.ErrorInvalidRegister(y);

        // Set most significant bit
        byte mostSignificant = (byte)((registers.v[x] >> 7) & 0x1);

        // Shift VX one bit to the left
        registers.v[x] = (byte)(registers.v[x] << 1);

        // Set VF to the value of the most significant bit of VX before the shift
        registers.v[0xF] = mostSignificant;
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

public static class Errors
{
    public static void ErrorOutOfBounds(UInt16 address)
    {
        Console.WriteLine($"Error: Address {address.ToString("X4")} is out of bounds.");
        Environment.Exit(1);
    }
    public static void ErrorInvalidOpcode(UInt16 opcode)
    {
        Console.WriteLine($"Error: Invalid opcode {opcode.ToString("X4")}");
        Environment.Exit(1);
    }
    public static void ErrorEmptyStack()
    {
        Console.WriteLine("Error: Stack is empty.");
        Environment.Exit(1);
    }
    public static void ErrorInvalidRegister(byte x)
    {
        Console.WriteLine($"Error: Invalid register {x.ToString("X2")}");
        Environment.Exit(1);
    }
}