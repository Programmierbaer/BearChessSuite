﻿using System;
using System.ComponentModel;
using www.SoLaNoSoft.com.BearChessBase.Implementations;
using www.SoLaNoSoft.com.BearChessBase;

namespace www.SoLaNoSoft.com.BearChessWin
{
    public interface IChessClocksWindow
    {
        event EventHandler TimeOutEvent;
        ClockTime GetClockTime();
        ClockTime GetCurrentTime();
        ClockTime GetElapsedTime();
        ClockTime GetDuration();
        void SetTime(ClockTime clockTime, int extraSeconds = 0);
        void SetTime(int hh, int mm, int ss, int extraSeconds = 0);
        void CorrectTime(int hh, int mm, int ss);
        void SetContinueTime(ClockTime clockTime, int extraSeconds = 0);
        void SetTooltip(string tooltip);
        void SetInfo(string info);
        void Reset();
        void Stop();
        void Go();
        void Show();
        void Hide();
        void Close();
        bool CountDown { get; set; }
        event CancelEventHandler Closing;
        event EventHandler Closed;
        double Top { get; set; }
        double Left { get; set; }
        double Height { get; set; }
        double Width { get; set; }
        void SetConfiguration(string capture, Configuration configuration);
    }
}