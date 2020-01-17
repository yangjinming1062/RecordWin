using System;
using System.Collections;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;

namespace RecordWin
{
    public class VideoManager
    {
        private int aviFile = 0;
        private ArrayList streams = new ArrayList();

        /// <summary>Open or create an AVI file</summary>
        /// <param name="fileName">Name of the AVI file</param>
        /// <param name="open">true: Open the file; false: Create or overwrite the file</param>
        public VideoManager(string fileName, bool open)
        {
            Avi.AVIFileInit();
            int result;

            if (open)//open existing file
            {
                result = Avi.AVIFileOpen(ref aviFile, fileName, Avi.OF_READWRITE, 0);
            }
            else//create empty file
            {
                result = Avi.AVIFileOpen(ref aviFile, fileName, Avi.OF_WRITE | Avi.OF_CREATE, 0);
            }

            if (result != 0)
            {
                throw new Exception("Exception in AVIFileOpen: " + result.ToString());
            }
        }

        /// <summary>Get the first video stream - usually there is only one video stream</summary>
		/// <returns>VideoStream object for the stream</returns>
		public VideoStream GetVideoStream()
        {
            IntPtr aviStream;

            int result = Avi.AVIFileGetStream(aviFile, out aviStream, Avi.streamtypeVIDEO, 0);

            if (result != 0)
            {
                throw new Exception("Exception in AVIFileGetStream: " + result.ToString());
            }

            VideoStream stream = new VideoStream(aviFile, aviStream);
            streams.Add(stream);
            return stream;
        }

        /// <summary>Getthe first wave audio stream</summary>
        /// <returns>AudioStream object for the stream</returns>
        private AudioStream GetWaveStream()
        {
            IntPtr aviStream;

            int result = Avi.AVIFileGetStream(aviFile, out aviStream, Avi.streamtypeAUDIO, 0);

            if (result != 0)
            {
                throw new Exception("Exception in AVIFileGetStream: " + result.ToString());
            }

            AudioStream stream = new AudioStream(aviFile, aviStream);
            streams.Add(stream);
            return stream;
        }

        /// <summary>Add an empty video stream to the file</summary>
        /// <param name="isCompressed">true: Create a compressed stream before adding frames</param>
        /// <param name="frameRate">Frames per second</param>
        /// <param name="frameSize">Size of one frame in bytes</param>
        /// <param name="width">Width of each image</param>
        /// <param name="height">Height of each image</param>
        /// <param name="format">PixelFormat of the images</param>
        /// <returns>VideoStream object for the new stream</returns>
        public VideoStream AddVideoStream(bool isCompressed, double frameRate, int frameSize, int width, int height, PixelFormat format)
        {
            VideoStream stream = new VideoStream(aviFile, isCompressed, frameRate, frameSize, width, height, format);
            streams.Add(stream);
            return stream;
        }

        /// <summary>Add an empty video stream to the file</summary>
        /// <remarks>Compresses the stream without showing the codecs dialog</remarks>
        /// <param name="compressOptions">Compression options</param>
        /// <param name="frameRate">Frames per second</param>
        /// <param name="firstFrame">Image to write into the stream as the first frame</param>
        /// <returns>VideoStream object for the new stream</returns>
        public VideoStream AddVideoStream(Avi.AVICOMPRESSOPTIONS compressOptions, double frameRate, Bitmap firstFrame)
        {
            VideoStream stream = new VideoStream(aviFile, compressOptions, frameRate, firstFrame);
            streams.Add(stream);
            return stream;
        }

        /// <summary>Add an empty video stream to the file</summary>
		/// <param name="isCompressed">true: Create a compressed stream before adding frames</param>
		/// <param name="frameRate">Frames per second</param>
		/// <param name="firstFrame">Image to write into the stream as the first frame</param>
		/// <returns>VideoStream object for the new stream</returns>
		public VideoStream AddVideoStream(bool isCompressed, double frameRate, Bitmap firstFrame)
        {
            VideoStream stream = new VideoStream(aviFile, isCompressed, frameRate, firstFrame);
            streams.Add(stream);
            return stream;
        }

        /// <summary>Add a wave audio stream from another file to this file</summary>
        /// <param name="waveFileName">Name of the wave file to add</param>
        /// <param name="startAtFrameIndex">Index of the video frame at which the sound is going to start</param>
        public void AddAudioStream(String waveFileName, int startAtFrameIndex)
        {
            VideoManager audioManager = new VideoManager(waveFileName, true);
            AudioStream newStream = audioManager.GetWaveStream();
            AddAudioStream(newStream, startAtFrameIndex);
            audioManager.Close();
        }

        private IntPtr InsertSilence(int countSilentSamples, IntPtr waveData, int lengthWave, ref Avi.AVISTREAMINFO streamInfo)
        {
            //initialize silence
            int lengthSilence = countSilentSamples * streamInfo.dwSampleSize;
            byte[] silence = new byte[lengthSilence];

            //initialize new sound
            int lengthNewStream = lengthSilence + lengthWave;
            IntPtr newWaveData = Marshal.AllocHGlobal(lengthNewStream);

            //copy silence
            Marshal.Copy(silence, 0, newWaveData, lengthSilence);

            //copy sound
            byte[] sound = new byte[lengthWave];
            Marshal.Copy(waveData, sound, 0, lengthWave);
            IntPtr startOfSound = new IntPtr(newWaveData.ToInt32() + lengthSilence);
            Marshal.Copy(sound, 0, startOfSound, lengthWave);

            Marshal.FreeHGlobal(newWaveData);

            streamInfo.dwLength = lengthNewStream;
            return newWaveData;
        }

        /// <summary>Add an existing wave audio stream to the file</summary>
        /// <param name="newStream">The stream to add</param>
        /// <param name="startAtFrameIndex">
        /// The index of the video frame at which the sound is going to start.
        /// '0' inserts the sound at the beginning of the video.
        /// </param>
        public void AddAudioStream(AudioStream newStream, int startAtFrameIndex)
        {
            Avi.AVISTREAMINFO streamInfo = new Avi.AVISTREAMINFO();
            Avi.PCMWAVEFORMAT streamFormat = new Avi.PCMWAVEFORMAT();
            int streamLength = 0;

            IntPtr rawData = newStream.GetStreamData(ref streamInfo, ref streamFormat, ref streamLength);
            IntPtr waveData = rawData;

            if (startAtFrameIndex > 0)
            {
                //not supported
                //streamInfo.dwStart = startAtFrameIndex;

                double framesPerSecond = GetVideoStream().FrameRate;
                double samplesPerSecond = newStream.CountSamplesPerSecond;
                double startAtSecond = startAtFrameIndex / framesPerSecond;
                int startAtSample = (int)(samplesPerSecond * startAtSecond);

                waveData = InsertSilence(startAtSample - 1, waveData, streamLength, ref streamInfo);
            }

            IntPtr aviStream;
            int result = Avi.AVIFileCreateStream(aviFile, out aviStream, ref streamInfo);
            if (result != 0)
            {
                throw new Exception("Exception in AVIFileCreateStream: " + result.ToString());
            }

            result = Avi.AVIStreamSetFormat(aviStream, 0, ref streamFormat, Marshal.SizeOf(streamFormat));
            if (result != 0)
            {
                throw new Exception("Exception in AVIStreamSetFormat: " + result.ToString());
            }

            result = Avi.AVIStreamWrite(aviStream, 0, streamLength, waveData, streamLength, Avi.AVIIF_KEYFRAME, 0, 0);
            if (result != 0)
            {
                throw new Exception("Exception in AVIStreamWrite: " + result.ToString());
            }

            result = Avi.AVIStreamRelease(aviStream);
            if (result != 0)
            {
                throw new Exception("Exception in AVIStreamRelease: " + result.ToString());
            }

            Marshal.FreeHGlobal(waveData);
        }

        /// <summary>Add an existing wave audio stream to the file</summary>
        /// <param name="waveData">The new stream's data</param>
        /// <param name="streamInfo">Header info for the new stream</param>
        /// <param name="streamFormat">The new stream' format info</param>
        /// <param name="streamLength">Length of the new stream</param>
        public void AddAudioStream(IntPtr waveData, Avi.AVISTREAMINFO streamInfo, Avi.PCMWAVEFORMAT streamFormat, int streamLength)
        {
            IntPtr aviStream;
            int result = Avi.AVIFileCreateStream(aviFile, out aviStream, ref streamInfo);
            if (result != 0)
            {
                throw new Exception("Exception in AVIFileCreateStream: " + result.ToString());
            }

            result = Avi.AVIStreamSetFormat(aviStream, 0, ref streamFormat, Marshal.SizeOf(streamFormat));
            if (result != 0)
            {
                throw new Exception("Exception in AVIStreamSetFormat: " + result.ToString());
            }

            result = Avi.AVIStreamWrite(aviStream, 0, streamLength, waveData, streamLength, Avi.AVIIF_KEYFRAME, 0, 0);
            if (result != 0)
            {
                throw new Exception("Exception in AVIStreamWrite: " + result.ToString());
            }

            result = Avi.AVIStreamRelease(aviStream);
            if (result != 0)
            {
                throw new Exception("Exception in AVIStreamRelease: " + result.ToString());
            }
        }

        /// <summary>Copy a piece of video and wave sound int a new file</summary>
        /// <param name="newFileName">File name</param>
        /// <param name="startAtSecond">Start copying at second x</param>
        /// <param name="stopAtSecond">Stop copying at second y</param>
        /// <returns>AviManager for the new video</returns>
        public VideoManager CopyTo(String newFileName, float startAtSecond, float stopAtSecond)
        {
            VideoManager newFile = new VideoManager(newFileName, false);

            try
            {
                //copy video stream

                VideoStream videoStream = GetVideoStream();

                int startFrameIndex = (int)(videoStream.FrameRate * startAtSecond);
                int stopFrameIndex = (int)(videoStream.FrameRate * stopAtSecond);

                videoStream.GetFrameOpen();
                Bitmap bmp = videoStream.GetBitmap(startFrameIndex);
                VideoStream newStream = newFile.AddVideoStream(false, videoStream.FrameRate, bmp);
                for (int n = startFrameIndex + 1; n <= stopFrameIndex; n++)
                {
                    bmp = videoStream.GetBitmap(n);
                    newStream.AddFrame(bmp);
                }
                videoStream.GetFrameClose();

                //copy audio stream

                AudioStream waveStream = GetWaveStream();

                Avi.AVISTREAMINFO streamInfo = new Avi.AVISTREAMINFO();
                Avi.PCMWAVEFORMAT streamFormat = new Avi.PCMWAVEFORMAT();
                int streamLength = 0;
                IntPtr ptrRawData = waveStream.GetStreamData(
                    ref streamInfo,
                    ref streamFormat,
                    ref streamLength);

                int startByteIndex = (int)(startAtSecond * (waveStream.CountSamplesPerSecond * streamFormat.nChannels * waveStream.CountBitsPerSample) / 8);
                int stopByteIndex = (int)(stopAtSecond * (waveStream.CountSamplesPerSecond * streamFormat.nChannels * waveStream.CountBitsPerSample) / 8);

                IntPtr ptrWavePart = new IntPtr(ptrRawData.ToInt32() + startByteIndex);

                byte[] rawData = new byte[stopByteIndex - startByteIndex];
                Marshal.Copy(ptrWavePart, rawData, 0, rawData.Length);
                Marshal.FreeHGlobal(ptrRawData);

                streamInfo.dwLength = rawData.Length;
                streamInfo.dwStart = 0;

                IntPtr unmanagedRawData = Marshal.AllocHGlobal(rawData.Length);
                Marshal.Copy(rawData, 0, unmanagedRawData, rawData.Length);
                newFile.AddAudioStream(unmanagedRawData, streamInfo, streamFormat, rawData.Length);
                Marshal.FreeHGlobal(unmanagedRawData);
            }
            catch (Exception ex)
            {
                newFile.Close();
                throw ex;
            }

            return newFile;
        }

        /// <summary>Release all ressources</summary>
		public void Close()
        {
            foreach (AviStream stream in streams)
            {
                stream.Close();
            }

            Avi.AVIFileRelease(aviFile);
            Avi.AVIFileExit();
        }
    }
}
