using System.Runtime.InteropServices;

namespace RecordWin
{
    /// <summary>
    /// 录音
    /// </summary>
    public class RecordSound
    {
        [DllImport("winmm.dll", EntryPoint = "mciSendString", CharSet = CharSet.Auto)]
        public static extern int mciSendString(string lpstrCommand, string lpstrReturnString, int uReturnLength, int hwndCallback);

        public void StartRecordSound()
        {
            //mciSendString("set wave bitpersample 8", "", 0, 0);
            //mciSendString("set wave samplespersec 20000", "", 0, 0);
            //mciSendString("set wave channels 2", "", 0, 0);
            //mciSendString("set wave format tag pcm", "", 0, 0);
            //mciSendString("open new type WAVEAudio alias movie", "", 0, 0);
            //mciSendString("record movie", "", 0, 0);
            mciSendString("close movie", "", 0, 0);
            mciSendString("open new type WAVEAudio alias movie", "", 0, 0);
            mciSendString("record movie", "", 0, 0);
        }
        public void EndRecordSound(string fileName)
        {
            mciSendString("stop movie", "", 0, 0);
            mciSendString("save movie " + fileName, "", 0, 0);
            mciSendString("close movie", "", 0, 0);
        }
    }
}
