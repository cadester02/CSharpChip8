using NAudio.Wave;
using NAudio.Wave.SampleProviders;

namespace Chip8.Services
{
    /// <summary>
    /// Audio states from playing to paused.
    /// </summary>
    public enum AudioState
    {
        Playing,
        Paused
    }

    /// <summary>
    /// Audio service that handles creating and playing a sin wave.
    /// </summary>
    public class AudioService
    {
        // Required to play audio
        private SignalGenerator _signalGenerator;
        private WaveOutEvent _waveOut;

        // Tracks the current state of the audio
        private AudioState _state;

        /// <summary>
        /// Constructor for the audio service.
        /// Initializes the wave and sets the output device.
        /// Sets the state to paused.
        /// </summary>
        public AudioService() 
        {
            _signalGenerator = new()
            {
                Gain = 0.2,
                Frequency = 500,
                Type = SignalGeneratorType.Sin
            };

            _waveOut = new WaveOutEvent();
            _waveOut.Init(_signalGenerator);

            _state = AudioState.Paused;
        }

        /// <summary>
        /// Plays the sin wave if not playing already.
        /// </summary>
        public void PlayAudio()
        {
            if (_state == AudioState.Playing) return;

            _waveOut.Play();
            _state = AudioState.Playing;
        }

        /// <summary>
        /// Pauses the sin wave if not paused already.
        /// </summary>
        public void StopAudio()
        {
            if (_state == AudioState.Paused) return;

            _waveOut.Pause();
            _state = AudioState.Paused;
        }
    }
}
