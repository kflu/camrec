using System;
namespace camrec
{
    interface ICameraRecorder
    {
        bool IsStopped();
        void RequestStop();
        bool ConfirmStop();
        void Start();
    }
}
