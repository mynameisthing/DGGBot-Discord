using System;
using DGGBot.Services.Eval.ResultModels;

namespace DGGBot.Services.Eval
{
    public class Globals
    {
        public Random Random { get; set; }
        public ConsoleLikeStringWriter Console { get; internal set; }

        public void ResetButton()
        {
            Environment.Exit(0);
        }
    }
}