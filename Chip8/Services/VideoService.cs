using Silk.NET.SDL;
using Silk.NET.Maths;

namespace Chip8.Services
{
    public class VideoService
    {
        private readonly Sdl _sdl = null!;
        private readonly unsafe Window* _window; // Use a pointer for the Window type
        private readonly unsafe Renderer* _renderer; // Use a pointer for the Renderer type

        private readonly int _scale;

        public unsafe VideoService(Sdl sdl, Window* window, Renderer* renderer, int scale)
        {
            _sdl = sdl;
            _window = window;
            _renderer = renderer;
            _scale = scale;
        }

        public unsafe void Render(bool[,] display)
        {
            _sdl.SetRenderDrawColor(_renderer, 0, 0, 0, 255); // Set background color to black  
            _sdl.RenderClear(_renderer); // Clear the screen

            _sdl.SetRenderDrawColor(_renderer, 255, 255, 255, 255); // Set pixel color to white  

            for (int col = 0; col < display.GetLength(0); col++)
            {
                for (int row = 0; row < display.GetLength(1); row++)
                {
                    if (display[col, row])
                    {
                        Rectangle<int> rect = new Rectangle<int>
                        {
                            Origin = new Vector2D<int>(col * _scale, row * _scale),
                            Size = new Vector2D<int>(_scale, _scale)
                        };

                        // Draw a rectangle for each pixel  
                        _sdl.RenderFillRect(_renderer, &rect);
                    }
                }
            }

            _sdl.RenderPresent(_renderer); // Update the screen
        }

        public unsafe void Dispose()
        {
            _sdl.DestroyRenderer(_renderer);
            _sdl.DestroyWindow(_window);
            _sdl.Quit();
        }
    }
}
