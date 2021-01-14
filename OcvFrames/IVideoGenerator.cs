using System.Drawing;

namespace OcvFrames
{
    public interface IVideoGenerator
    {
        /// <summary>
        /// Draw the next frame. Return 'true' if more frames to come, 'false' if end of video.
        /// If you don't clear, the frame will be based on the previous one.
        /// </summary>
        bool DrawFrame(int frameNumber, Graphics g);
    }
}