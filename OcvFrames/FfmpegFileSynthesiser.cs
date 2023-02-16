using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Threading.Tasks;
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
    private int _videoFrameNumber;

    /// <summary> Default path when installing using `choco install ffmpeg` </summary>
    //private const string FfmpegPath = @"C:\ProgramData\chocolatey\lib\ffmpeg\tools";


    public FfmpegFileSynthesiser(string filePath, int width, int height, int framesPerSecond)
    {
        _width = width;
        _height = height;
        _framesPerSecond = framesPerSecond;
        _outputPath = Path.GetFullPath(filePath);
        _frameImage = new Bitmap(width, height, PixelFormat.Format24bppRgb);
    }

    IEnumerable<IVideoFrame> CreateVideoFrames()
    {
        if (_source is null) yield break;

        using var g = Graphics.FromImage(_frameImage);

        var more = true;
        _videoFrameNumber = 0;
        while (more)
        {
            more = _source.DrawFrame(_videoFrameNumber++, g);
            var wrapper = new BitmapVideoFrameWrapper(_frameImage); // we do NOT dispose of this frame, as that kills the source bitmap and nothing else
            Console.Write('?');
            yield return wrapper;
        }
    }

    private IEnumerable<IAudioSample> CreateAudioFrames()
    {
        if (_source is null) yield break;
        
        var more = true;
        var i = 0;
        while (more)
        {
            more = _source.GetAudioSamples(_videoFrameNumber, i++, out var buffer);
            if (buffer is null) yield break;
            
            yield return new PcmAudioSampleWrapper(buffer);
            //Console.Write('a');
        }
    }

    public void WriteVideo(IVideoGenerator source)
    {
        _source = source;
        var videoFramesSource = new RawVideoPipeSource(CreateVideoFrames())
        {
            FrameRate = _framesPerSecond
        };

        // https://trac.ffmpeg.org/wiki/audio%20types
        var audioSource = new RawAudioPipeSource(CreateAudioFrames())
        {
            Channels = 1, SampleRate = 44100, Format = "u8" // PCM unsigned 8-bit
        };

        FFMpegArguments
            .FromTwinPipeInput(videoFramesSource, audioSource, _width, _height)
            //.FromPipeInput(videoFramesSource)
            //.AddPipeInput(audioSource)
            .OutputToFile(_outputPath, overwrite: true, options => options
                .WithVideoCodec(VideoCodec.LibX264)
                .WithAudioCodec(AudioCodec.Aac)
            )
            //.ProcessAsynchronously(); // ??? Doesn't seem to allow sync
            .ProcessSynchronously();
    }

    public void Dispose()
    {
        _frameImage.Dispose();
    }
}