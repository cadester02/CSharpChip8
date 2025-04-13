namespace Chip8.Chip8Core
{

    public class Chip8Core
    {
        // Sets debug mode
        public bool debug { get; private set; } = false;

        // Data Registers range from V0 to VF
        // 16 in total
        public byte[] v = new byte[16]; // V0 to VF

        // Address register
        // Can only be loaded with a 12 bit memory address
        public UInt16 i = 0x000;

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
            // Write the font to memory addresses starting at 0x00
            Array.Copy(Constants.Constants.FONT, 0, memory, 0x00, Constants.Constants.FONT.Length);
        }

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
            UInt16 opcode = FetchOpcode(pc);
            // Increment Program Counter
            pc += 0x2;

            DecodeInstruction(opcode);
        }

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
                        // 5XY0
                        case 0x0:
                            // Skip following instruction if VX == VY
                            Execute5XY0(x, y);
                            break;
                        default:
                            // Invalid opcode
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
                            // Invalid opcode
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
                            // Invalid opcode
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
                    // Set VF to 1 if pixel is set to off
                    ExecuteDXYN(x, y, (byte)(opcode & 0xF));
                    break;
                case 0xE:
                    switch (opcode & 0xFF)
                    {
                        case 0x9E:
                            // Skip next instruction if key with the value of VX is pressed
                            break;
                        case 0xA1:
                            // Skip next instruction if key with the value of VX is not pressed
                            break;
                        default:
                            // Invalid opcode
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
                            // Wait for a key press and store the value of the key in VX
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
                            // Invalid opcode
                            InvalidOpcode(opcode);
                            break;
                    }
                    break;
            }
        }

        public void Execute0NNN(UInt16 address)
        {
            // Execute instruction at NNN
            if (debug) Console.WriteLine($"CHIP8: Executing 0NNN, Running opcode at 0x0{address.ToString("X3")}.");

            // Get opcode from memory
            UInt16 opcode = FetchOpcode(address);

            // Run opcode
            DecodeInstruction(opcode);
        }

        public void Execute00E0()
        {
            if (debug) Console.WriteLine("CHIP8: Executing 00E0, Clearing screen.");

            // Clear the screen
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
            // Return from a subroutine
            if (debug) Console.WriteLine("CHIP8: Executing 00EE, Returning from subroutine.");

            // Make sure stack is not empty
            if (stack.Count == 0)
                ErrorService.HandleError(ErrorType.EmptyStack, "Stack is empty, cannot return from subroutine.");

            // Pop the top address from the stack
            UInt16 address = stack.Pop();

            // Set program counter to address
            pc = address;
        }

        public void Execute1NNN(UInt16 address)
        {
            // Jump to NNN
            if (debug) Console.WriteLine($"CHIP8: Executing 1NNN, Jumping to 0x0{address.ToString("X3")}.");

            // Set program counter to address
            pc = address;
        }

        public void Execute2NNN(UInt16 address)
        {
            // Execute subroutine at NNN
            if (debug) Console.WriteLine($"CHIP8: Executing 2NNN, Executing subroutine at 0x0{address.ToString("X3")}.");

            // Push the current program counter to the stack
            stack.Push(pc);

            // Set program counter to address
            pc = address;
        }

        public void Execute3XNN(byte x, byte nn)
        {
            // Skip next instruction if VX == NN
            if (debug) Console.WriteLine($"CHIP8: Executing 3XNN, Skipping next instruction if V{x.ToString("X1")} == {nn.ToString("X2")}.");
            CheckRegisterBounds(x);

            if (v[x] == nn)
            {
                // Skip next instruction
                pc += 0x2;
            }
        }

        public void Execute4XNN(byte x, byte nn)
        {
            // Skip next instruction if VX != NN
            if (debug) Console.WriteLine($"CHIP8: Executing 4XNN, Skipping next instruction if V{x.ToString("X1")} != {nn.ToString("X2")}.");
            CheckRegisterBounds(x);

            if (v[x] != nn)
            {
                // Skip next instruction
                pc += 0x2;
            }
        }

        public void Execute5XY0(byte x, byte y)
        {
            // Skip next instruction if VX == VY
            if (debug) Console.WriteLine($"CHIP8: Executing 5XY0, Skipping next instruction if V{x.ToString("X1")} == V{y.ToString("X1")}.");
            CheckRegisterBounds(x, y);

            if (v[x] == v[y])
            {
                // Skip next instruction
                pc += 0x2;
            }
        }

        public void Execute6XNN(byte x, byte nn)
        {
            // Store NN in VX
            if (debug) Console.WriteLine($"CHIP8: Executing 6XNN, Storing {nn.ToString("X2")} in V{x.ToString("X1")}.");
            CheckRegisterBounds(x);

            // Store nn in vx
            v[x] = nn;
        }

        public void Execute7XNN(byte x, byte nn)
        {
            // Add NN to VX
            if (debug) Console.WriteLine($"CHIP8: Executing 7XNN, Adding {nn.ToString("X2")} to V{x.ToString("X1")}.");
            CheckRegisterBounds(x);

            // Add nn to vx
            v[x] += nn;
        }

        public void Execute8XY0(byte x, byte y)
        {
            // Set VX to VY
            if (debug) Console.WriteLine($"CHIP8: Executing 8XY0, Setting V{x.ToString("X1")} to V{y.ToString("X1")}.");
            CheckRegisterBounds(x, y);

            // Set vx to vy
            v[x] = v[y];
        }

        public void Execute8XY1(byte x, byte y)
        {
            // Set VX to bitwise OR of VX and VY
            if (debug) Console.WriteLine($"CHIP8: Executing 8XY1, Setting V{x.ToString("X1")} to V{x.ToString("X1")} | V{y.ToString("X1")}.");
            CheckRegisterBounds(x, y);

            // Set vx to vx | vy
            v[x] = (byte)(v[x] | v[y]);

            // Quirk Reset VF
            v[0xF] = 0;
        }

        public void Execute8XY2(byte x, byte y)
        {
            // Set VX to bitwise AND of VX and VY
            if (debug) Console.WriteLine($"CHIP8: Executing 8XY2, Setting V{x.ToString("X1")} to V{x.ToString("X1")} & V{y.ToString("X1")}.");
            CheckRegisterBounds(x, y);

            // Set vx to vx & vy
            v[x] = (byte)(v[x] & v[y]);

            // Quirk Reset VF
            v[0xF] = 0;
        }

        public void Execute8XY3(byte x, byte y)
        {
            // Set VX to bitwise XOR of VX and VY
            if (debug) Console.WriteLine($"CHIP8: Executing 8XY3, Setting V{x.ToString("X1")} to V{x.ToString("X1")} ^ V{y.ToString("X1")}.");
            CheckRegisterBounds(x, y);

            // Set vx to vx ^ vy
            v[x] = (byte)(v[x] ^ v[y]);

            // Quirk Reset VF
            v[0xF] = 0;
        }

        public void Execute8XY4(byte x, byte y)
        {
            if (debug) Console.WriteLine($"CHIP8: Executing 8XY4, Adding V{y.ToString("X1")} to V{x.ToString("X1")}.");
            CheckRegisterBounds(x, y);

            // Add VY to VX
            UInt16 sum = (UInt16)(v[x] + v[y]);

            // Set VX to the sum
            v[x] = (byte)(sum & 0xFF);

            // Set VF to 1 if there is a carry else 0
            v[0xF] = sum > 0xFF ? (byte)0x1 : (byte)0x0;
        }

        public void Execute8XY5(byte x, byte y)
        {
            // Subtract VY from VX
            if (debug) Console.WriteLine($"CHIP8: Executing 8XY5, Subtracting V{y.ToString("X1")} from V{x.ToString("X1")}.");
            CheckRegisterBounds(x, y);

            // Set borrow
            byte borrow = v[x] >= v[y] ? (byte)0x1 : (byte)0x0;

            // Subtract VY from VX
            v[x] -= v[y];

            // Set VF to 0 if there is a borrow else 1
            v[0xF] = borrow;
        }

        public void Execute8XY6(byte x, byte y)
        {
            // Set VX to VY and shift VX one bit to the right. Set VF to the value of the least significant bit of VX before the shift.
            if (debug) Console.WriteLine($"CHIP8: Executing 8XY6, Shifting V{x.ToString("X1")} one bit to the right.");
            CheckRegisterBounds(x, y);

            // Set least significant bit
            byte leastSignificant = (byte)(v[x] & 0x1);

            // Shift VX one bit to the right
            v[x] = (byte)(v[x] >> 1);

            // Set VF to the value of the least significant bit of VX before the shift
            v[0xF] = leastSignificant;
        }

        public void Execute8XY7(byte x, byte y)
        {
            // Set VX to the result of subtracting VX from VY. VF is set to 0 if there is a borrow, and 1 if there is not.
            if (debug) Console.WriteLine($"CHIP8: Executing 8XY7, Subtracting V{x.ToString("X1")} from V{y.ToString("X1")}.");
            CheckRegisterBounds(x, y);

            // Set borrow
            byte borrow = v[y] >= v[x] ? (byte)0x1 : (byte)0x0;

            // Subtract VX from VY
            v[x] = (byte)(v[y] - v[x]);

            // Set VF to 0 if there is a borrow else 1
            v[0xF] = borrow;
        }

        public void Execute8XYE(byte x, byte y)
        {
            // Set VX to VY and shift VX one bit to the left. Set VF to the value of the most significant bit of VX before the shift.
            if (debug) Console.WriteLine($"CHIP8: Executing 8XYE, Shifting V{x.ToString("X1")} one bit to the left.");
            CheckRegisterBounds(x, y);

            // Set most significant bit
            byte mostSignificant = (byte)((v[x] >> 7) & 0x1);

            // Shift VX one bit to the left
            v[x] = (byte)(v[x] << 1);

            // Set VF to the value of the most significant bit of VX before the shift
            v[0xF] = mostSignificant;
        }

        public void Execute9XY0(byte x, byte y)
        {
            // Skip next instruction if VX != VY
            if (debug) Console.WriteLine($"CHIP8: Executing 9XY0, Skipping next instruction if V{x.ToString("X1")} != V{y.ToString("X1")}.");
            CheckRegisterBounds(x, y);

            if (v[x] != v[y])
            {
                // Skip next instruction
                pc += 0x2;
            }
        }

        public void ExecuteANNN(UInt16 address)
        {
            // Set I to NNN
            if (debug) Console.WriteLine($"CHIP8: Executing ANNN, Setting I to 0x0{address.ToString("X3")}.");

            // Set I to address
            i = address;
        }

        public void ExecuteBNNN(UInt16 address)
        {
            // Jump to NNN + V0
            if (debug) Console.WriteLine($"CHIP8: Executing BNNN, Jumping to 0x0{address.ToString("X3")} + V0.");

            // Set program counter to address + V0
            UInt16 jumpAddress = (UInt16)(address + v[0]);
            pc = jumpAddress;
        }

        public void ExecuteCXNN(byte x, byte nn)
        {
            // Set VX to a random number AND NN
            if (debug) Console.WriteLine($"CHIP8: Executing CXNN, Setting V{x.ToString("X1")} to a random number AND {nn.ToString("X2")}.");
            CheckRegisterBounds(x);

            // Set vx to a random number AND nn
            Random rand = new Random();
            v[x] = (byte)(rand.Next(0x00, 0xFF) & nn);
        }

        public void ExecuteDXYN(byte x, byte y, byte n)
        {
            if (debug) Console.WriteLine($"CHIP8: Executing DXYN, Drawing sprite at V{x.ToString("X1")}, V{y.ToString("X1")} with {n.ToString("X2")} bytes of sprite data starting at address I.");
            CheckRegisterBounds(x, y);

            byte xPos = (byte)(v[x] % 64);
            byte yPos = (byte)(v[y] % 32);

            // Set VF to 0
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
                    bool currentSpritePixel = (spriteData >> (7 - col) & 0b1) == 0b1;

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


        public void ExecuteFX07(byte x)
        {
            // Store the current value of the delay timer in VX
            if (debug) Console.WriteLine($"CHIP8: Executing FX07, Storing delay timer in V{x.ToString("X1")}.");
            CheckRegisterBounds(x);

            // Store the current value of the delay timer in VX
            v[x] = delayTimer;
        }

        public void ExecuteFX15(byte x)
        {
            // Set the delay timer to VX
            if (debug) Console.WriteLine($"CHIP8: Executing FX15, Setting delay timer to V{x.ToString("X1")}.");
            CheckRegisterBounds(x);

            // Set the delay timer to VX
            delayTimer = v[x];
        }

        public void ExecuteFX18(byte x)
        {
            // Set the sound timer to VX
            if (debug) Console.WriteLine($"CHIP8: Executing FX18, Setting sound timer to V{x.ToString("X1")}.");
            CheckRegisterBounds(x);

            // Set the sound timer to VX
            soundTimer = v[x];
        }

        public void ExecuteFX1E(byte x)
        {
            // Add VX to I
            if (debug) Console.WriteLine($"CHIP8: Executing FX1E, Adding V{x.ToString("X1")} to I.");
            CheckRegisterBounds(x);

            // Add VX to I
            i += v[x];
        }

        public void ExecuteFX29(byte x)
        {
            // Set I to the location of the sprite for the character in VX.
            if (debug) Console.WriteLine($"CHIP8: Executing FX29, Setting I to the location of the sprite for V{x.ToString("X1")}.");
            CheckRegisterBounds(x);

            // Set I to the location of the sprite for the character in VX
            i = (UInt16)(v[x] * 5);
        }

        public void ExecuteFX33(byte x)
        {
            // Store BCD representation of VX in memory locations I, I+1, and I+2
            if (debug) Console.WriteLine($"CHIP8: Executing FX33, Storing BCD representation of V{x.ToString("X1")} in memory locations I, I+1, and I+2.");
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
            // Store registers V0 through VX in memory starting at address I
            if (debug) Console.WriteLine($"CHIP8: Executing FX55, Storing registers V0 through V{x.ToString("X1")} in memory starting at address I.");
            CheckAddressBounds(i);

            // Store registers V0 through VX in memory starting at address I
            for (byte reg = 0; reg <= x; reg++)
            {
                CheckAddressBounds((UInt16)(i + reg));

                memory[i + reg] = v[reg];
            }
        }

        public void ExecuteFX65(byte x)
        {
            // Read registers V0 through VX from memory starting at address I
            if (debug) Console.WriteLine($"CHIP8: Executing FX65, Reading registers V0 through V{x.ToString("X1")} from memory starting at address I.");
            CheckRegisterBounds(x);

            // Read registers V0 through VX from memory starting at address I
            for (byte reg = 0; reg <= x; reg++)
            {
                CheckAddressBounds((UInt16)(i + reg));

                v[reg] = memory[i + reg];
            }
        }

        public void DisplayScreen()
        {
            Console.WriteLine("\nCHIP8: Displaying screen.\n");
            for (int row = 0; row < display.GetLength(1); row++)
            {
                for (int col = 0; col < display.GetLength(0); col++)
                {
                    Console.Write(display[col, row] ? "██" : "  ");
                }
                Console.WriteLine();
            }
        }

        public Chip8Core(ArgumentService arguments)
        {
            debug = arguments.debugMode;

            LoadFont();
            LoadFile(arguments.filePath);
        }
    }
}
