using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using FFMpegCore;
using FFMpegCore.Enums;
using FFMpegCore.Extend;
using FFMpegCore.Extensions.System.Drawing.Common;
using FFMpegCore.Pipes;
using PixelFormat = System.Drawing.Imaging.PixelFormat;

namespace OcvFrames;

/// <summary>
/// Write a sequence of video frames and audio samples
/// to a video file.
/// Requires FFMPEG to be installed. See https://github.com/rosenbjerg/FFMpegCore#installation
/// </summary>
public class FfmpegFileSynthesiser : IDisposable
{
    private readonly int _width;
    private readonly int _height;
    private readonly int _framesPerSecond;
    private readonly string _outputPath;
    private IVideoGenerator? _source;
    private readonly Bitmap _frameImage;

    /// <summary> Default path when installing using `choco install ffmpeg` </summary>
    private const string FfmpegPath = @"C:\ProgramData\chocolatey\lib\ffmpeg\tools";
        
        
    public FfmpegFileSynthesiser(string filePath, int width, int height, int framesPerSecond)
    {
        _width = width;
        _height = height;
        _framesPerSecond = framesPerSecond;
        _outputPath = Path.GetFullPath(filePath);
        _frameImage = new Bitmap(width, height, PixelFormat.Format24bppRgb);
    }
        
    IEnumerable<IVideoFrame> CreateFrames()
    {
        if (_source is null) yield break;
        
        Console.WriteLine("First video frame requested");
        
        using var g = Graphics.FromImage(_frameImage);
            
        var more = true;
        var i = 0;
        while (more)
        {
            more = _source.DrawFrame(i++, g);
            var frame = new BitmapVideoFrameWrapper(_frameImage); // we do NOT dispose of this frame, as that kills the source bitmap and nothing else
            yield return frame;
        }
    }

    private IEnumerator<IAudioSample> CreateAudio()
    {
        var toneSamples = 44100 / 440;
        var sample = new byte[toneSamples];
        var r = Math.PI * 2.0 / toneSamples;

        for (int i = 0; i < toneSamples; i++)
        {
            sample[i] = (byte)((Math.Sin(i+r) * 64) + 127);
        }
        
        Console.WriteLine("First audio frame requested");
        
        for (int i = 0; i < 440; i++) // one second of audio
        {
            yield return new PcmAudioSampleWrapper(sample);
        }
    }

    public void WriteVideo(IVideoGenerator source)
    {
        _source = source;
        var videoFramesSource = new RawVideoPipeSource(CreateFrames())
        {
            FrameRate = _framesPerSecond
        };
        
        // https://trac.ffmpeg.org/wiki/audio%20types
        var audioSource = new RawAudioPipeSource(CreateAudio()){
            Channels = 1, SampleRate = 44100, Format = "u8" // PCM unsigned 8-bit
        };
            
        FFMpegArguments
            .FromPipeInput(videoFramesSource)
            .AddPipeInput(audioSource)
            .OutputToFile(_outputPath, overwrite:true, options => options
                .WithVideoCodec(VideoCodec.LibX264)
                .WithAudioCodec(AudioCodec.Aac)
            )
            .ProcessSynchronously();
    }

    public void Dispose()
    {
        _frameImage.Dispose();
    }
}