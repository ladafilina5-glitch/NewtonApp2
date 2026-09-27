using System;

namespace NewtonApp.Models
{
    public class SolutionRecord
    {
        public int Id { get; set; }
        public DateTime SolutionTime { get; set; }
        public string Equation { get; set; } = "";
        public double InitialGuess { get; set; }
        public double Root { get; set; }
        public int IterationsCount { get; set; }
        public double Error { get; set; }
    }
}