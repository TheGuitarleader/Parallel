// Copyright 2025 Kyle Ebbinga

namespace Parallel.Server.Attributes
{
    [AttributeUsage(AttributeTargets.Class)]
    public class DescriptionAttribute : Attribute
    {
        public string Text { get; }

        public DescriptionAttribute(string text) => Text = text;
    }
}