using Silk.NET.SDL;
using Silk.NET.Maths;

namespace Chip8.Services
{
    public class SDLService
    {
        private static Sdl _sdl = null!;
        private static unsafe Window* _window; // Use a pointer for the Window type
        private static unsafe Renderer* _renderer; // Use a pointer for the Renderer type
        private bool[] _keypad = new bool[16];

        private readonly int _scale;

        public unsafe SDLService(int scale)
        {
            _scale = scale;

            for (int key = 0; key < _keypad.Length; key++)
            {
                _keypad[key] = false;
            }
        }

        public unsafe void StartSDL()
        {
            _sdl = Sdl.GetApi();

            // Initialize SDL
            if (_sdl.Init(Sdl.InitVideo) != 0)
            {
                ErrorService.HandleError(ErrorType.UnexpectedError, "Failed to initialize SDL.");
            }

            // Create window
            _window = _sdl.CreateWindow(
                "Chip8 Emulator",
                100,
                100,
                Constants.Constants.SCREEN_WIDTH * _scale,
                Constants.Constants.SCREEN_HEIGHT * _scale,
                (uint)WindowFlags.Shown);

            // Create renderer
            _renderer = _sdl.CreateRenderer(_window, -1, (uint)RendererFlags.Accelerated);
        }

        public unsafe void RenderScreen(bool[,] display)
        {
            Texture* texture = _sdl.CreateTexture(_renderer,
                (uint)PixelFormatEnum.Rgba8888,
                (int)TextureAccess.Target,
                Constants.Constants.SCREEN_WIDTH,
                Constants.Constants.SCREEN_HEIGHT);

            UInt32* pixels = stackalloc UInt32[Constants.Constants.SCREEN_WIDTH * Constants.Constants.SCREEN_HEIGHT];
            for (int i = 0; i < Constants.Constants.SCREEN_WIDTH * Constants.Constants.SCREEN_HEIGHT; i++)
            {
                pixels[i] = display[i % Constants.Constants.SCREEN_WIDTH, i / Constants.Constants.SCREEN_WIDTH] ? 0xFFFFFFFF : 0x00000000;
            }

            _sdl.UpdateTexture(texture, null, pixels, Constants.Constants.SCREEN_WIDTH * sizeof(UInt32));

            _sdl.SetRenderDrawColor(_renderer, 0, 0, 0, 255); 
            _sdl.RenderClear(_renderer);
            _sdl.RenderCopy(_renderer, texture, null, null);
            _sdl.RenderPresent(_renderer);
        }

        public unsafe bool[] HandleKeys()
        {
            Event sdlEvent = new Event(); // Create a new SDL event

            if (_sdl.PollEvent(&sdlEvent) != 0)
            {
                switch (sdlEvent.Type)
                {
                    case (uint)EventType.Quit:
                        Dispose();
                        Environment.Exit(0);
                        break;

                    case (uint)EventType.Keydown:
                    case (uint)EventType.Keyup:
                        KeyUpdate(sdlEvent.Type, sdlEvent.Key.Keysym.Sym);
                        break;
                }
            }

            return _keypad;
        }

        private void KeyUpdate(uint eventType, int key)
        {
            bool keyDown = eventType == (uint)EventType.Keydown;

            switch (key)
            {
                case (int)KeyCode.K1:
                    _keypad[0x1] = keyDown;
                    break;

                case (int)KeyCode.K2:
                    _keypad[0x2] = keyDown;
                    break;

                case (int)KeyCode.K3:
                    _keypad[0x3] = keyDown;
                    break;

                case (int)KeyCode.K4:
                    _keypad[0xC] = keyDown;
                    break;

                case (int)KeyCode.KQ:
                    _keypad[0x4] = keyDown;
                    break;

                case (int)KeyCode.KW:
                    _keypad[0x5] = keyDown;
                    break;

                case (int)KeyCode.KE:
                    _keypad[0x6] = keyDown;
                    break;

                case (int)KeyCode.KR:
                    _keypad[0xD] = keyDown;
                    break;

                case (int)KeyCode.KA:
                    _keypad[0x7] = keyDown;
                    break;

                case (int)KeyCode.KS:
                    _keypad[0x8] = keyDown;
                    break;

                case (int)KeyCode.KD:
                    _keypad[0x9] = keyDown;
                    break;

                case (int)KeyCode.KF:
                    _keypad[0xE] = keyDown;
                    break;

                case (int)KeyCode.KZ:
                    _keypad[0xA] = keyDown;
                    break;

                case (int)KeyCode.KX:
                    _keypad[0x0] = keyDown;
                    break;

                case (int)KeyCode.KC:
                    _keypad[0xB] = keyDown;
                    break;

                case (int)KeyCode.KV:
                    _keypad[0xF] = keyDown;
                    break;
            }
        }

        public unsafe void Dispose()
        {
            _sdl.DestroyRenderer(_renderer);
            _sdl.DestroyWindow(_window);
            _sdl.Quit();
        }
    }
}
