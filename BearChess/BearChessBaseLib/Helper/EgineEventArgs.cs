using System;

namespace www.SoLaNoSoft.com.BearChessBase
{
    public class EngineEventArgs : EventArgs
    {
        public string Name { get; }
        public string FromEngine { get; }
        public int Color { get; }
        public bool FirstEngine { get; }
        public bool BuddyEngine { get; }
        public bool ValidForAnalysis { get; }
        public bool ProbingEngine { get; }
        public int EngineIndex {get; }

        public EngineEventArgs(string name, string fromEngine, int color, bool firstEngine, bool buddyEngine, bool probingEngine, bool validForAnalysis, int engineIndex)
        {

            Name = name;
            FromEngine = fromEngine;
            Color = color;
            FirstEngine = firstEngine;
            BuddyEngine = buddyEngine;
            ProbingEngine = probingEngine;
            ValidForAnalysis = validForAnalysis;
            EngineIndex = engineIndex;
        }

        public override string ToString()
        {
            return $"Name: {Name}  FromEngine: {FromEngine} Color: {Color}  FirstEngine: {FirstEngine}  BuddyEngine: {BuddyEngine}  ProbingEngine: {ProbingEngine}";
        }
    }
}
