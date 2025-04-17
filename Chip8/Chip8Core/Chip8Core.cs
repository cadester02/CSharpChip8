namespace Chip8.Chip8Core
{

    public class Chip8Core
    {
        private readonly bool debug = false;    // If in debug mode will print to the screen the instruction it runs

        private byte[] v = new byte[16]; // V0 to VF

        private UInt16 i = 0x000;   // The I register

        private byte[] memory = new byte[4096]; // System memory 4kb

        public bool[,] display { get; private set; } = new bool[64, 32];   // Display 64 x 32

        private UInt16 pc = 0x200;  // Program counter

        private Stack<UInt16> stack = new();    // Stack height of 12

        private byte delayTimer = 0;    // The delay timer

        public byte soundTimer { get; private set; } = 0;  // The sound timer

        private bool[] keys = new bool[16]; // The previous frame keys


        /// <summary>
        /// Constructor for the Chip 8 Core.
        /// Sets the debug mode and clears the keys.
        /// Loads the font and file into memory.
        /// </summary>
        /// <param name="arguments">Arguments passed in from the ArgumentService.</param>
        public Chip8Core(ArgumentService arguments)
        {
            debug = arguments.DebugMode;
            for (int key = 0; key < keys.Length; key++)
            {
                keys[key] = false;
            }

            LoadFont();
            LoadFile(arguments.FilePath);
        }


        /// <summary>
        /// Loads a file into memory at 0x200.
        /// </summary>
        /// <param name="fileName">The file that will be loaded.</param>
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

        /// <summary>
        /// Loads the font into memory, starting at 0x000.
        /// </summary>
        private void LoadFont()
        {
            // Write the font to memory addresses, placing it at 0x00
            Array.Copy(Constants.Constants.FONT, 0, memory, 0x00, Constants.Constants.FONT.Length);
        }

        /// <summary>
        /// Called when an invalid opcode is used.
        /// </summary>
        /// <param name="opcode">The opcode that caused the error.</param>
        public void InvalidOpcode(UInt16 opcode)
        {
            ErrorService.HandleError(ErrorType.InvalidOpcode, $"Invalid opcode {opcode.ToString("X4")}.");
        }

        /// <summary>
        /// Checks the register to make sure it is within bounds.
        /// </summary>
        /// <param name="x">The value of the register being checked.</param>
        public void CheckRegisterBounds(byte x)
        {
            if (x > 0xF)
                ErrorService.HandleError(ErrorType.InvalidRegister, $"Invalid register V{x.ToString("X1")}.");
        }

        /// <summary>
        /// Checks both the x and y registers to see if they are within bounds.
        /// </summary>
        /// <param name="x">The x register being checked.</param>
        /// <param name="y">The y register being checked.</param>
        public void CheckRegisterBounds(byte x, byte y)
        {
            if (x > 0xF)
                ErrorService.HandleError(ErrorType.InvalidRegister, $"Invalid register V{x.ToString("X1")}.");
            if (y > 0xF)
                ErrorService.HandleError(ErrorType.InvalidRegister, $"Invalid register V{y.ToString("X1")}.");
        }

        /// <summary>
        /// Returns a valid address by masking the given address with 0xFFF.
        /// </summary>
        /// <param name="address">The supplied address.</param>
        /// <returns>A valid 12bit address.</returns>
        private UInt16 ValidateAddress(UInt16 address)
        {
            return (UInt16)(address & 0xFFF);
        }

        /// <summary>
        /// Checks the stack to see if it is empty.
        /// Called while returning from subroutine.
        /// </summary>
        public void CheckEmptyStack()
        {
            if (stack.Count == 0)
                ErrorService.HandleError(ErrorType.EmptyStack, "Stack is empty, cannot return from subroutine.");
        }

        /// <summary>
        /// Checks the stack to see if it is full.
        /// Called while going to a subroutine.
        /// </summary>
        public void CheckFullStack()
        {
            if (stack.Count == Constants.Constants.STACK_HEIGHT)
                ErrorService.HandleError(ErrorType.StackOverflow, "Stack is full, cannot push to stack.");
        }

        /// <summary>
        /// Fetches the current opcode at the address given.
        /// </summary>
        /// <param name="address">The address where the opcode is being fetched.</param>
        /// <returns>The opcode that was fetched.</returns>
        public UInt16 FetchOpcode(UInt16 address)
        {
            // Get instruction
            byte highByte = memory[ValidateAddress(address)];
            byte lowByte = memory[ValidateAddress((UInt16)(address + 1))];

            // Create the opcode.
            UInt16 opcode = (UInt16)((highByte << 8) | lowByte);

            return opcode;
        }

        /// <summary>
        /// Runs the chip8 loop.
        /// Decrements the timers.
        /// Runs 11 instructions per frame. If DXYN waits breaks the loop.
        /// Updates the keypad.
        /// </summary>
        /// <param name="keypad">The current keys pressed.</param>
        public void RunChip8(bool[] keypad)
        {
            // Decrement timers
            if (delayTimer > 0)
                delayTimer--;
            if (soundTimer > 0)
                soundTimer--;

            for (int ins = 0; ins < Constants.Constants.INSTRUCTIONS_PER_FRAME; ins++)
            {
                UInt16 opcode = FetchOpcode(pc);
                // Increment Program Counter
                pc += 0x2;

                if (debug) Console.WriteLine($"CHIP8: Executing opcode {opcode.ToString("X4")}.");
                DecodeInstruction(opcode, keypad);

                // If drawing a sprite to the screen
                if (((opcode >> 12) & 0xF) == 0xD)
                {
                    break;
                }
            }

            // Update keypad
            keys = (bool[])keypad.Clone();
        }

        /// <summary>
        /// Contains a switch to decode each chip8 instruction.
        /// Decodes the first nibble then branches based off there.
        /// Executes the instruction it decodes.
        /// </summary>
        /// <param name="opcode">The opcode to be decoded and executed.</param>
        /// <param name="keypad">The current keys pressed and released.</param>
        public void DecodeInstruction(UInt16 opcode, bool[] keypad)
        {
            // Variables required for instructions
            byte instructionNibble = (byte)((opcode >> 12) & 0xF);

            byte x = (byte)((opcode >> 8) & 0xF);
            byte y = (byte)((opcode >> 4) & 0xF);

            byte n = (byte)(opcode & 0xF);
            byte nn = (byte)(opcode & 0xFF);
            UInt16 nnn = (UInt16)(opcode & 0xFFF);

            // Switch to decode and execute instructions
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
                            Execute0NNN(nnn, keypad);
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
                            // Set VX to bitwise OR of VX and VY reset VF
                            Execute8XY1(x, y);
                            break;
                        case 0x2:
                            // Set VX to bitwise AND of VX and VY reset VF
                            Execute8XY2(x, y);
                            break;
                        case 0x3:
                            // Set VX to bitwise XOR of VX and VY reset VF
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
                            Execute8XY6(x, y);
                            break;
                        case 0x7:
                            // Set VX to the result of subtracting VX from VY.
                            // Set VF to 0 if there is a borrow else 1.
                            Execute8XY7(x, y);
                            break;
                        case 0xE:
                            // Set VX to VY and shift VX one bit to the left. Set VF to the value of the most significant bit of VX before the shift.
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
                    // Set VX to a random number bitwise AND NN
                    ExecuteCXNN(x, nn);
                    break;
                case 0xD:
                    // Draw sprite to screen. Don't execute any other instructions until screen is drawn.
                    ExecuteDXYN(x, y, (byte)(opcode & 0xF));
                    break;
                case 0xE:
                    switch (opcode & 0xFF)
                    {
                        case 0x9E:
                            // Skip next instruction if key with the value of VX is pressed
                            ExecuteEX9E(x, keypad);
                            break;
                        case 0xA1:
                            // Skip next instruction if key with the value of VX is not pressed
                            ExecuteEXA1(x, keypad);
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
                            ExecuteFX0A(x, keypad);
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
                            // Set I to the location of the sprite for the character in VX. Sprite is 5 lines high
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

        /// <summary>
        /// Executes the instruction at the given address.
        /// </summary>
        /// <param name="address">The address to execute.</param>
        /// <param name="keypad">The current keys pressed.</param>
        public void Execute0NNN(UInt16 address, bool[] keypad)
        {
            // Get opcode from memory
            UInt16 opcode = FetchOpcode(address);

            // Run opcode
            DecodeInstruction(opcode, keypad);
        }

        /// <summary>
        /// Clears the screen.
        /// </summary>
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

        /// <summary>
        /// Returns from the subroutine.
        /// </summary>
        public void Execute00EE()
        {
            CheckEmptyStack();

            // Pop the top address from the stack
            UInt16 address = stack.Pop();

            // Set program counter to address
            pc = address;
        }

        /// <summary>
        /// Jumps to the address given.
        /// </summary>
        /// <param name="address">The address to jump to.</param>
        public void Execute1NNN(UInt16 address)
        {
            // Set program counter to address
            pc = address;
        }

        /// <summary>
        /// Go to subroutine at the given address.
        /// </summary>
        /// <param name="address">The address of the subroutine.</param>
        public void Execute2NNN(UInt16 address)
        {
            CheckFullStack();

            stack.Push(pc);
            pc = address;
        }

        /// <summary>
        /// Skip next instruction if VX == NN.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="nn">The one byte number NN.</param>
        public void Execute3XNN(byte x, byte nn)
        {
            CheckRegisterBounds(x);

            if (v[x] == nn)
            {
                // Skip next instruction
                pc += 0x2;
            }
        }

        /// <summary>
        /// Skip next instruction if VX != NN.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="nn">The one byte number NN.</param>
        public void Execute4XNN(byte x, byte nn)
        {
            CheckRegisterBounds(x);

            if (v[x] != nn)
            {
                // Skip next instruction
                pc += 0x2;
            }
        }

        /// <summary>
        /// Skip next instruction if VX == VY.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="y">The register VY.</param>
        public void Execute5XY0(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            if (v[x] == v[y])
            {
                // Skip next instruction
                pc += 0x2;
            }
        }

        /// <summary>
        /// Set VX to NN.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="nn">The one byte number NN.</param>
        public void Execute6XNN(byte x, byte nn)
        {
            CheckRegisterBounds(x);

            v[x] = nn;
        }

        /// <summary>
        /// Add NN to VX.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="nn">The one byte number NN.</param>
        public void Execute7XNN(byte x, byte nn)
        {
            CheckRegisterBounds(x);

            v[x] += nn;
        }

        /// <summary>
        /// Set VX to VY.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="y">The register VY.</param>
        public void Execute8XY0(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            v[x] = v[y];
        }

        /// <summary>
        /// Set VX to bitwise OR of VX OR VY.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="y">The register VY.</param>
        public void Execute8XY1(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            v[x] = (byte)(v[x] | v[y]);

            // Quirk Reset VF
            v[0xF] = 0;
        }

        /// <summary>
        /// Set VX to bitwise AND VX AND VY.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="y">The register VY.</param>
        public void Execute8XY2(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            v[x] = (byte)(v[x] & v[y]);

            // Quirk Reset VF
            v[0xF] = 0;
        }

        /// <summary>
        /// Set VX to bitwise XOR VX XOR VY.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="y">The register VY.</param>
        public void Execute8XY3(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            v[x] = (byte)(v[x] ^ v[y]);

            // Quirk Reset VF
            v[0xF] = 0;
        }

        /// <summary>
        /// Add VY to VX, VF is set to 1 if overflow else 0.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="y">The register VY.</param>
        public void Execute8XY4(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            UInt16 sum = (UInt16)(v[x] + v[y]);

            v[x] = (byte)(sum & 0xFF);

            // Set VF to 1 if there is a carry else 0
            v[0xF] = sum > 0xFF ? (byte)0x1 : (byte)0x0;
        }

        /// <summary>
        /// Subtract VY from VX, VF is set to 0 if a borrow else 1.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="y">The register VY.</param>
        public void Execute8XY5(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            // Set borrow
            byte borrow = v[x] >= v[y] ? (byte)0x1 : (byte)0x0;

            v[x] -= v[y];

            // Set VF to 0 if there is a borrow else 1
            v[0xF] = borrow;
        }

        /// <summary>
        /// Set VX to VY and shift VX one bit to the right. VF is set to bit shifted out.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="y">The register VY.</param>
        public void Execute8XY6(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            // Set VX to VY
            v[x] = v[y];

            // Set least significant bit
            byte leastSignificant = (byte)(v[x] & 0x1);

            v[x] = (byte)(v[x] >> 1);

            // Set VF to the value of the least significant bit of VX before the shift
            v[0xF] = leastSignificant;
        }

        /// <summary>
        /// Set VX to the result of subtractiong VX from VY. VF is set to 0 if borrow else 1.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="y">The register VY.</param>
        public void Execute8XY7(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            // Set borrow
            byte borrow = v[y] >= v[x] ? (byte)0x1 : (byte)0x0;

            v[x] = (byte)(v[y] - v[x]);

            // Set VF to 0 if there is a borrow else 1
            v[0xF] = borrow;
        }

        /// <summary>
        /// Set VX to VY and shift VX one bit to the left. VF is set to the bit shifted out.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="y">The register VY.</param>
        public void Execute8XYE(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            // Set VX to VY
            v[x] = v[y];

            // Set most significant bit
            byte mostSignificant = (byte)((v[x] >> 7) & 0x1);

            v[x] = (byte)(v[x] << 1);

            // Set VF to the value of the most significant bit of VX before the shift
            v[0xF] = mostSignificant;
        }

        /// <summary>
        /// Skip the next instruction if VX != VY.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="y">The register VY.</param>
        public void Execute9XY0(byte x, byte y)
        {
            CheckRegisterBounds(x, y);

            if (v[x] != v[y])
            {
                // Skip next instruction
                pc += 0x2;
            }
        }

        /// <summary>
        /// Set I to NNN.
        /// </summary>
        /// <param name="address">The memory address NNN.</param>
        public void ExecuteANNN(UInt16 address)
        {
            i = address;
        }

        /// <summary>
        /// Jump to address NNN + V0.
        /// </summary>
        /// <param name="address">The memory address NNN.</param>
        public void ExecuteBNNN(UInt16 address)
        {
            UInt16 jumpAddress = (UInt16)(address + v[0]);
            pc = jumpAddress;
        }

        /// <summary>
        /// Set VX to a random value then bitwise AND VX AND NN.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="nn">The one byte number NN.</param>
        public void ExecuteCXNN(byte x, byte nn)
        {
            CheckRegisterBounds(x);

            Random rand = new Random();
            v[x] = (byte)(rand.Next(0x00, 0xFF) & nn);
        }

        /// <summary>
        /// Draw a 8xN sprite to the screen at position VX, VY. The sprite data starts at I.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="y">The register VX.</param>
        /// <param name="n">The nibble value N.</param>
        public void ExecuteDXYN(byte x, byte y, byte n)
        {
            CheckRegisterBounds(x, y);

            // Get screen position
            byte xPos = (byte)(v[x] % 64);
            byte yPos = (byte)(v[y] % 32);

            // Reset VF
            v[0xF] = 0;

            // Loop through N
            for (int row = 0; row < n; row++)
            {
                // If at the edge stop drawing
                if ((yPos + row) >= display.GetLength(1))
                    break;

                // Pull sprite data
                byte spriteData = memory[ValidateAddress((UInt16)(i + row))];

                // Column is 8 pixels wide
                for (int col = 0; col < 8; col++)
                {
                    // If at the edge stop drawing
                    if ((xPos + col) >= display.GetLength(0))
                        break;

                    // Get current display pixel and current sprite pixel
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

        /// <summary>
        /// Skip the next instruction if the lower nibble of VX is pressed.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="keypad">The keypad input data.</param>
        public void ExecuteEX9E(byte x, bool[] keypad)
        {
            CheckRegisterBounds(x);

            byte lowerNibble = (byte)(v[x] & 0xF);

            if (keypad[lowerNibble])
                pc += 0x2;  // Skip instruction
        }

        /// <summary>
        /// Skip the next instruction if the lower nibble of VX is not pressed.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="keypad">The keypad input data.</param>
        public void ExecuteEXA1(byte x, bool[] keypad)
        {
            CheckRegisterBounds(x);

            byte lowerNibble = (byte)(v[x] & 0xF);

            if (!keypad[lowerNibble])
                pc += 0x2;  // Skip instruction
        }

        /// <summary>
        /// Set VX to the delay timer.
        /// </summary>
        /// <param name="x">The register VX.</param>
        public void ExecuteFX07(byte x)
        {
            CheckRegisterBounds(x);

            v[x] = delayTimer;
        }

        /// <summary>
        /// Wait for a key release, set VX to the key released.
        /// If no key release decrement PC.
        /// </summary>
        /// <param name="x">The register VX.</param>
        /// <param name="keypad">The keypad input data.</param>
        public void ExecuteFX0A(byte x, bool[] keypad)
        {
            CheckRegisterBounds(x);

            for (int key = 0; key < keys.Length; key++)
            {
                // if a key is released
                if (keys[key] && !keypad[key])
                {
                    v[x] = (byte)key;
                    return;
                }
            }

            pc -= 0x2;  // If no keys released decrement pc
        }

        /// <summary>
        /// Set the delay timer to VX.
        /// </summary>
        /// <param name="x">The register VX.</param>
        public void ExecuteFX15(byte x)
        {
            CheckRegisterBounds(x);

            delayTimer = v[x];
        }

        /// <summary>
        /// Set the sound timer to VX.
        /// </summary>
        /// <param name="x">The register VX.</param>
        public void ExecuteFX18(byte x)
        {
            CheckRegisterBounds(x);

            soundTimer = v[x];
        }

        /// <summary>
        /// Add VX to I.
        /// </summary>
        /// <param name="x">The register VX.</param>
        public void ExecuteFX1E(byte x)
        {
            CheckRegisterBounds(x);

            i += v[x];
        }

        /// <summary>
        /// Set I to the 5 line high hex sprite at the lowest nibble in VX.
        /// </summary>
        /// <param name="x">The register VX.</param>
        public void ExecuteFX29(byte x)
        {
            CheckRegisterBounds(x);

            // Set I to the location of the sprite for the character in VX
            i = (UInt16)((v[x] & 0xF) * 5);
        }

        /// <summary>
        /// Write the BCD value of VX to the addresses of I, I+1, I+2.
        /// </summary>
        /// <param name="x">The register VX.</param>
        public void ExecuteFX33(byte x)
        {
            CheckRegisterBounds(x);

            // Store BCD representation of VX in memory locations I, I+1, and I+2
            memory[ValidateAddress(i)] = (byte)(v[x] / 100); // Get hundreths position. 255 / 100 = 2
            memory[ValidateAddress((UInt16)(i + 1))] = (byte)((v[x] / 10) % 10);   // Get tenths position. 255 / 10 = 25 % 10 = 5
            memory[ValidateAddress((UInt16)(i + 2))] = (byte)(v[x] % 10);  // Get ones position. 255 % 10 = 5
        }

        /// <summary>
        /// Write from V0 to VX at the addresses pointed to by incrementing I.
        /// I is incremented by X + 1.
        /// </summary>
        /// <param name="x">The register VX.</param>
        public void ExecuteFX55(byte x)
        {
            // Store registers V0 through VX in memory starting at address I
            for (byte reg = 0; reg <= x; reg++)
            {
                memory[ValidateAddress((UInt16)(i + reg))] = v[reg];
            }

            i += (ushort)(x + 1);
        }

        /// <summary>
        /// Set V0 to VX to the values at the addresses obtained by incrementing I.
        /// </summary>
        /// <param name="x">The register VX.</param>
        public void ExecuteFX65(byte x)
        {
            //CheckRegisterBounds(x);

            // Read registers V0 through VX from memory starting at address I
            for (byte reg = 0; reg <= x; reg++)
            {
                v[reg] = memory[ValidateAddress((UInt16)(i + reg))];
            }

            i += (ushort)(x + 1);
        }
    }
}
