using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
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
    private readonly int _framesPerSecond;
    private readonly string _outputPath;
    private IVideoGenerator? _source;
    private readonly Bitmap _frameImage;
    private int _videoFrameNumber;

    /// <summary> Default path when installing using `choco install ffmpeg` </summary>
    //private const string FfmpegPath = @"C:\ProgramData\chocolatey\lib\ffmpeg\tools";


    public FfmpegFileSynthesiser(string filePath, int width, int height, int framesPerSecond)
    {
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
            yield return wrapper;
        }
    }

    private IEnumerable<IAudioSample> CreateAudioFrames()
    {
        if (_source is null) return Array.Empty<IAudioSample>();
        return _source.GetAudioSamples().Chunk(44100).Select(buf => new PcmAudioSampleWrapper(buf));
    }

    public void WriteVideo(IVideoGenerator source)
    {
        _source = source;
        var videoFramesSource = new RawVideoPipeSource(CreateVideoFrames())
        {
            FrameRate = _framesPerSecond
        };

        var tempName = Path.GetDirectoryName(_outputPath)
                       + Path.DirectorySeparatorChar
                       + Path.GetFileNameWithoutExtension(_outputPath)
                       + "_tmp_"
                       + Path.GetExtension(_outputPath);
        Console.WriteLine("Temp output="+tempName);

        // Generate the video into a media file
        FFMpegArguments
            .FromPipeInput(videoFramesSource)
            .OutputToFile(tempName, overwrite: true, options => options
                .WithVideoCodec(VideoCodec.LibX264)
            )
            .ProcessSynchronously();
         
        // https://trac.ffmpeg.org/wiki/audio%20types
        var audioSource = new RawAudioPipeSource(CreateAudioFrames())
        {
            Channels = 1, SampleRate = 44100, Format = "u8" // PCM unsigned 8-bit
        };
        
        // add audio track to it
        FFMpegArguments
            .FromFileInput(tempName)
            .AddPipeInput(audioSource)
            .OutputToFile(_outputPath, overwrite: true, options => options
                .WithAudioCodec(AudioCodec.Aac)
            )
            .ProcessSynchronously();
        
        File.Delete(tempName);
    }

    public void Dispose()
    {
        _frameImage.Dispose();
    }
}