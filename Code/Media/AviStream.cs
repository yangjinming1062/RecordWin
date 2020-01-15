using System;

namespace RecordWin
{
    public abstract class AviStream
    {
        protected int aviFile;
        protected IntPtr aviStream;
        protected IntPtr compressedStream;
        protected bool writeCompressed;

        /// <summary>Pointer to the unmanaged AVI file</summary>
        internal int FilePointer => aviFile;

        /// <summary>Pointer to the unmanaged AVI Stream</summary>
        internal virtual IntPtr StreamPointer => aviStream;

        /// <summary>Flag: The stream is compressed/uncompressed</summary>
        internal bool WriteCompressed => writeCompressed;

        /// <summary>Close the stream</summary>
        public virtual void Close()
        {
            if (writeCompressed)
            {
                Avi.AVIStreamRelease(compressedStream);
            }
            Avi.AVIStreamRelease(StreamPointer);
        }

        /// <summary>Export the stream into a new file</summary>
        /// <param name="fileName"></param>
		public abstract void ExportStream(String fileName);
    }
}
