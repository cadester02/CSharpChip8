namespace Chip8.Chip8Core
{

    public class Chip8Core
    {
        public bool debug { get; private set; } = false;

        public byte[] v = new byte[16]; // V0 to VF

        public UInt16 i = 0x000;

        public byte[] memory { get; private set; } = new byte[4096];

        public bool[,] display { get; private set; } = new bool[64, 32];

        public UInt16 pc { get; private set; } = 0x200;

        public Stack<UInt16> stack { get; private set; } = new Stack<UInt16>();

        public byte delayTimer { get; private set; } = 0;

        public byte soundTimer { get; private set; } = 0;

        private bool[] keys = new bool[16];
        private bool[] prevKeys = new bool[16];

        /*
         * INITIALIZATION
         */

        public Chip8Core(ArgumentService arguments)
        {
            debug = arguments.debugMode;
            for (int key = 0; key < keys.Length; key++)
            {
                keys[key] = false;
                prevKeys[key] = false;
            }

            LoadFont();
            LoadFile(arguments.filePath);
        }

        /*
         * LOADING
         */

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
            // Write the font to memory addresses, placing it at 0x00
            Array.Copy(Constants.Constants.FONT, 0, memory, 0x00, Constants.Constants.FONT.Length);
        }

        /*
         * DEBUG AND ERRORS
         */

        public void InvalidOpcode(UInt16 opcode)
        {
            ErrorService.HandleError(ErrorType.InvalidOpcode, $"Invalid opcode {opcode.ToString("X4")}.");
        }

        public void CheckRegisterBounds(byte x)
        {
            if (x > 0xF)
                ErrorService.HandleError(ErrorType.InvalidRegister, $"Invalid register V{x.ToString("X1")}.");
        }

        public void CheckRegisterBounds(byte x, byte y)
        {
            if (x > 0xF)
                ErrorService.HandleError(ErrorType.InvalidRegister, $"Invalid register V{x.ToString("X1")}.");
            if (y > 0xF)
                ErrorService.HandleError(ErrorType.InvalidRegister, $"Invalid register V{y.ToString("X1")}.");
        }

        public void CheckAddressBounds(UInt16 address)
        {
            if (address >= memory.Length)
                ErrorService.HandleError(ErrorType.OutOfBounds, $"Address {address.ToString("X4")} is out of bounds.");
        }

        public void CheckEmptyStack()
        {
            if (stack.Count == 0)
                ErrorService.HandleError(ErrorType.EmptyStack, "Stack is empty, cannot return from subroutine.");
        }

        public void CheckFullStack()
        {
            if (stack.Count == 12)
                ErrorService.HandleError(ErrorType.StackOverflow, "Stack is full, cannot push to stack.");
        }

        /*
         * CHIP8 Functions
         */

        public UInt16 FetchOpcode(UInt16 address)
        {
            CheckAddressBounds(address);
            CheckAddressBounds((UInt16)(address + 1));

            // Get instruction
            byte highByte = memory[address];
            byte lowByte = memory[address + 1];

            // Create the opcode.
            UInt16 opcode = (UInt16)((highByte << 8) | lowByte);

            return opcode;
        }

        public void RunChip8()
        {
            for (int ins = 0; ins < 11; ins++)
            {
                UInt16 opcode = FetchOpcode(pc);
                // Increment Program Counter
                pc += 0x2;

                if (debug) Console.WriteLine($"CHIP8: Executing opcode {opcode.ToString("X4")}.");
                DecodeInstruction(opcode);

                // If drawing a sprite to the screen
                if (((opcode >> 12) & 0xF) == 0xD)
                {
                    break;
                }
            }
        }

        public void DecrementTimers()
        {
            if (delayTimer > 0)
                delayTimer--;

            if (soundTimer > 0)
                soundTimer--;
        }

        public void UpdateKeypad(bool[] keypad)
        {
            prevKeys = (bool[])keys.Clone();
            keys = (bool[])keypad.Clone();
        }

        public void DecodeInstruction(UInt16 opcode)
        {
            byte instructionNibble = (byte)((opcode >> 12) & 0xF);

            byte x = (byte)((opcode >> 8) & 0xF);
            byte y = (byte)((opcode >> 4) & 0xF);

            byte n = (byte)(opcode & 0xF);
            byte nn = (byte)(opcode & 0xFF);
            UInt16 nnn = (UInt16)(opcode & 0xFFF);

            switch (instructionNibble)
            {
                case 0x0:
                    switch (nnn)
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
                            Execute0NNN(nnn);
                            break;
                    }
                    break;

                case 0x1:
                    // Jump to NNN
                    Execute1NNN(nnn);
                    break;

                case 0x2:
                    // Execute subroutine at NNN
                    Execute2NNN(nnn);
                    break;

                case 0x3:
                    // Skip following instruction if VX == NN
                    Execute3XNN(x, nn);
                    break;

                case 0x4:
                    // Skip following instruction if VX != NN
                    Execute4XNN(x, nn);
                    break;

                case 0x5:
                    switch (opcode & 0xF)
                    {
                        case 0x0:
                            // Skip following instruction if VX == VY
                            Execute5XY0(x, y);
                            break;
                        default:
                            InvalidOpcode(opcode);
                            break;
                    }
                    break;

                case 0x6:
                    // Store NN in VX
                    Execute6XNN(x, nn);
                    break;

                case 0x7:
                    // Add NN to VX
                    Execute7XNN(x, nn);
                    break;

                case 0x8:
                    switch (opcode & 0xF)
                    {
                        case 0x0:
                            // Set VX to VY
                            Execute8XY0(x, y);
                            break;
                        case 0x1:
                            // Set VX to bitwise OR of VX and VY
                            // Quirk Reset VF
                            Execute8XY1(x, y);
                            break;
                        case 0x2:
                            // Set VX to bitwise AND of VX and VY
                            // Quirk Reset VF
                            Execute8XY2(x, y);
                            break;
                        case 0x3:
                            // Set VX to bitwise XOR of VX and VY
                            // Quirk Reset VF
                            Execute8XY3(x, y);
                            break;
                        case 0x4:
                            // Add VY to VX
                            // Set VF to 1 if there is a carry else 0
                            Execute8XY4(x, y);
                            break;
                        case 0x5:
                            // Subtract VY from VX
                            // Set VF to 0 if there is a borrow else 1
                            Execute8XY5(x, y);
                            break;
                        case 0x6:
                            // Set VX to VY and shift VX one bit to the right. Set VF to the value of the least significant bit of VX before the shift.
                            // Quirk Don't set VX to VY only shift VX
                            Execute8XY6(x, y);
                            break;
                        case 0x7:
                            // Set VX to the result of subtracting VX from VY. VF is set to 0 if there is a borrow, and 1 if there is not.
                            // Set VF to 0 if there is a borrow else 1
                            Execute8XY7(x, y);
                            break;
                        case 0xE:
                            // Set VX to VY and shift VX one bit to the left. Set VF to the value of the most significant bit of VX before the shift.
                            // Quirk Don't set VX to VY only shift VX
                            Execute8XYE(x, y);
                            break;
                        default:
                            InvalidOpcode(opcode);
                            break;
                    }
                    break;

                case 0x9:
                    switch (opcode & 0xF)
                    {
                        case 0x0:
                            // Skip next instruction if VX != VY
                            Execute9XY0(x, y);
                            break;
                        default:
                            InvalidOpcode(opcode);
                            break;
                    }
                    break;
                case 0xA:
                    // Set I to NNN
                    ExecuteANNN(nnn);
                    break;
                case 0xB:
                    // Jump to NNN + V0
                    ExecuteBNNN(nnn);
                    break;
                case 0xC:
                    // Set VX to a random number AND NN
                    ExecuteCXNN(x, nn);
                    break;
                case 0xD:
                    // Draw sprite to screen.
                    ExecuteDXYN(x, y, (byte)(opcode & 0xF));
                    break;
                case 0xE:
                    switch (opcode & 0xFF)
                    {
                        case 0x9E:
                            // Skip next instruction if key with the value of VX is pressed
                            ExecuteEX9E(x);
                            break;
                        case 0xA1:
                            // Skip next instruction if key with the value of VX is not pressed
                            ExecuteEXA1(x);
                            break;
                        default:
                            InvalidOpcode(opcode);
                            break;
                    }
                    break;
                case 0xF:
                    switch (opcode & 0xFF)
                    {
                        case 0x07:
                            // Store the current value of the delay timer in VX
                            ExecuteFX07(x);
                            break;
                        case 0x0A:
                            // Wait for a key release and store the value of the key in VX
                            ExecuteFX0A(x);
                            break;
                        case 0x15:
                            // Set the delay timer to VX
                            ExecuteFX15(x);
                            break;
                        case 0x18:
                            // Set the sound timer to VX
                            ExecuteFX18(x);
                            break;
                        case 0x1E:
                            // Add VX to I
                            ExecuteFX1E(x);
                            break;
                        case 0x29:
                            // Set I to the location of the sprite for the character in VX. Characters 0-F (in hexadecimal) are represented by a 4x5 font.
                            ExecuteFX29(x);
                            break;
                        case 0x33:
                            // Store BCD representation of VX in memory locations I, I+1, and I+2
                            ExecuteFX33(x);
                            break;
                        case 0x55:
                            // Store registers V0 through VX in memory starting at address I
                            ExecuteFX55(x);
                            break;
                        case 0x65:
                            // Read registers V0 through VX from memory starting at address I
                            ExecuteFX65(x);
                            break;
                        default:
                            InvalidOpcode(opcode);
                            break;
                    }
                    break;
            }
        }

        public void Execute0NNN(UInt16 address)
        {
            // Get opcode from memory
            UInt16 opcode = FetchOpcode(address);

            // Run opcode
            DecodeInstruction(opcode);
        }

        public void Execute00E0()
        {
            for (int row = 0; row < display.GetLength(0); row++)
            {
                for (int col = 0; col < display.GetLength(1); col++)
                {
                    display[row, col] = false;
                }
            }
        }

        public void Execute00EE()
        {
            CheckEmptyStack();

            // Pop the top address from the stack
            UInt16 address = stack.Pop();

            // Set program counter to address
            pc = address;
        }

        public void Execute1NNN(UInt16 address)
        {
            // Set program counter to address
            pc = address;
        }

        public void Execute2NNN(UInt16 address)
        {
            CheckFullStack();

            stack.Push(pc);
            pc = address;
        }

        public void Execute3XNN(byte x, byte nn)
        {
            CheckRegisterBounds(x);

            if (v[x] == nn)
            {
                // Skip next instruction
                pc += 0x2;
            }
        }

        public void Execute4XNN(byte x, byte nn)
        {
            CheckRegisterBounds(x);

            if (v[x] != nn)
            {
                // Skip next instruction
                pc += 0x2;
            }
        }

        public void Execute5XY0(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            if (v[x] == v[y])
            {
                // Skip next instruction
                pc += 0x2;
            }
        }

        public void Execute6XNN(byte x, byte nn)
        {
            CheckRegisterBounds(x);

            v[x] = nn;
        }

        public void Execute7XNN(byte x, byte nn)
        {
            CheckRegisterBounds(x);

            v[x] += nn;
        }

        public void Execute8XY0(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            v[x] = v[y];
        }

        public void Execute8XY1(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            v[x] = (byte)(v[x] | v[y]);

            // Quirk Reset VF
            v[0xF] = 0;
        }

        public void Execute8XY2(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            v[x] = (byte)(v[x] & v[y]);

            // Quirk Reset VF
            v[0xF] = 0;
        }

        public void Execute8XY3(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            v[x] = (byte)(v[x] ^ v[y]);

            // Quirk Reset VF
            v[0xF] = 0;
        }

        public void Execute8XY4(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            UInt16 sum = (UInt16)(v[x] + v[y]);

            v[x] = (byte)(sum & 0xFF);

            // Set VF to 1 if there is a carry else 0
            v[0xF] = sum > 0xFF ? (byte)0x1 : (byte)0x0;
        }

        public void Execute8XY5(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            // Set borrow
            byte borrow = v[x] >= v[y] ? (byte)0x1 : (byte)0x0;

            v[x] -= v[y];

            // Set VF to 0 if there is a borrow else 1
            v[0xF] = borrow;
        }

        public void Execute8XY6(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            // QUIRK
            v[x] = v[y];

            // Set least significant bit
            byte leastSignificant = (byte)(v[x] & 0x1);

            v[x] = (byte)(v[x] >> 1);

            // Set VF to the value of the least significant bit of VX before the shift
            v[0xF] = leastSignificant;
        }

        public void Execute8XY7(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            // Set borrow
            byte borrow = v[y] >= v[x] ? (byte)0x1 : (byte)0x0;

            v[x] = (byte)(v[y] - v[x]);

            // Set VF to 0 if there is a borrow else 1
            v[0xF] = borrow;
        }

        public void Execute8XYE(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            // QUIRK
            v[x] = v[y];

            // Set most significant bit
            byte mostSignificant = (byte)((v[x] >> 7) & 0x1);

            v[x] = (byte)(v[x] << 1);

            // Set VF to the value of the most significant bit of VX before the shift
            v[0xF] = mostSignificant;
        }

        public void Execute9XY0(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            if (v[x] != v[y])
            {
                // Skip next instruction
                pc += 0x2;
            }
        }

        public void ExecuteANNN(UInt16 address)
        {
            i = address;
        }

        public void ExecuteBNNN(UInt16 address)
        {
            UInt16 jumpAddress = (UInt16)(address + v[0]);
            pc = jumpAddress;
        }

        public void ExecuteCXNN(byte x, byte nn)
        {
            CheckRegisterBounds(x);

            Random rand = new Random();
            v[x] = (byte)(rand.Next(0x00, 0xFF) & nn);
        }

        public void ExecuteDXYN(byte x, byte y, byte n)
        {
            CheckRegisterBounds(x, y);

            byte xPos = (byte)(v[x] % 64);
            byte yPos = (byte)(v[y] % 32);

            v[0xF] = 0;

            for (int row = 0; row < n; row++)
            {
                if ((yPos + row) >= display.GetLength(1))
                    break;

                CheckAddressBounds((UInt16)(i + row));

                byte spriteData = memory[i + row];

                for (int col = 0; col < 8; col++)
                {
                    if ((xPos + col) >= display.GetLength(0))
                        break;

                    bool currentPixel = display[xPos + col, yPos + row];
                    bool currentSpritePixel = (spriteData >> (7 - col) & 0x1) == 0x1;

                    if (currentPixel && currentSpritePixel)
                    {
                        // If the pixel is already set, set VF to 1
                        v[0xF] = 1;

                        // Set the pixel to off
                        display[xPos + col, yPos + row] = false;
                    }

                    if (!currentPixel && currentSpritePixel)
                    {
                        // If the pixel is not set, set it to on
                        display[xPos + col, yPos + row] = true;
                    }
                }
            }
        }

        public void ExecuteEX9E(byte x)
        {
            CheckRegisterBounds(x);

            byte lowerNibble = (byte)(v[x] & 0xF);

            if (keys[lowerNibble])
                pc += 0x2;  // Skip instruction
        }

        public void ExecuteEXA1(byte x)
        {
            CheckRegisterBounds(x);

            byte lowerNibble = (byte)(v[x] & 0xF);

            if (!keys[lowerNibble])
                pc += 0x2;  // Skip instruction
        }

        public void ExecuteFX07(byte x)
        {
            CheckRegisterBounds(x);

            v[x] = delayTimer;
        }

        public void ExecuteFX0A(byte x)
        {
            CheckRegisterBounds(x);

            for (int key = 0; key < keys.Length; key++)
            {
                // if a key is released
                if (prevKeys[key] && !keys[key])
                {
                    v[x] = (byte)key;
                    return;
                }
            }

            pc -= 0x2;  // If no keys released decrement pc
        }

        public void ExecuteFX15(byte x)
        {
            CheckRegisterBounds(x);

            delayTimer = v[x];
        }

        public void ExecuteFX18(byte x)
        {
            CheckRegisterBounds(x);

            soundTimer = v[x];
        }

        public void ExecuteFX1E(byte x)
        {
            CheckRegisterBounds(x);

            i += v[x];
        }

        public void ExecuteFX29(byte x)
        {
            CheckRegisterBounds(x);

            // Set I to the location of the sprite for the character in VX
            i = (UInt16)(v[x] * 5);
        }

        public void ExecuteFX33(byte x)
        {
            CheckRegisterBounds(x);

            for (int mem = 0; mem < 3; mem++)
            {
                CheckAddressBounds((UInt16)(i + mem));
            }

            // Store BCD representation of VX in memory locations I, I+1, and I+2
            memory[i] = (byte)(v[x] / 100); // Get hundreths position. 255 / 100 = 2
            memory[i + 1] = (byte)((v[x] / 10) % 10);   // Get tenths position. 255 / 10 = 25 % 10 = 5
            memory[i + 2] = (byte)(v[x] % 10);  // Get ones position. 255 % 10 = 5
        }

        public void ExecuteFX55(byte x)
        {
            CheckAddressBounds(i);

            // Store registers V0 through VX in memory starting at address I
            for (byte reg = 0; reg <= x; reg++)
            {
                CheckAddressBounds((UInt16)(i + reg));

                memory[i + reg] = v[reg];
            }

            i += (ushort)(x + 1);
        }

        public void ExecuteFX65(byte x)
        {
            CheckRegisterBounds(x);

            // Read registers V0 through VX from memory starting at address I
            for (byte reg = 0; reg <= x; reg++)
            {
                CheckAddressBounds((UInt16)(i + reg));

                v[reg] = memory[i + reg];
            }

            i += (ushort)(x + 1);
        }

        public void DisplayScreen()
        {
            for (int row = 0; row < display.GetLength(1); row++)
            {
                for (int col = 0; col < display.GetLength(0); col++)
                {
                    Console.Write(display[col, row] ? "██" : "  ");
                }
                Console.WriteLine();
            }
        }
    }
}
