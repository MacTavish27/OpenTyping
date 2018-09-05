﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Windows.Documents;
using System.Windows.Media;
using LiveCharts;
using LiveCharts.Wpf;
using MahApps.Metro.Controls;

namespace OpenTyping
{
    /// <summary>
    /// SentencePracticeWindow.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class SentencePracticeWindow : MetroWindow, INotifyPropertyChanged
    {
        private string currentText;
        public string CurrentText
        {
            get => currentText;
            set => SetField(ref currentText, value);
        }

        private readonly PracticeData practiceData;
        private readonly bool shuffle;

        private readonly TypingMeasurer typingMeasurer = new TypingMeasurer();

        private int typingSpeed;
        public int TypingSpeed
        {
            get => typingSpeed;
            private set => SetField(ref typingSpeed, value);
        }

        private int accuracy;
        public int Accuracy
        {
            get => accuracy;
            private set => SetField(ref accuracy, value);
        }

        public SeriesCollection TypingSpeedCollection { get; set; } = new SeriesCollection
        {
            new LineSeries
            {
                Title = "타속",
                Values = new ChartValues<int>()
            }
        };

        private int? currentSentenceIndex;
        private static readonly Random SentenceIndexRandom = new Random();

        private readonly Brush correctBackground = Brushes.LightGreen;
        private readonly Brush incorrectBackground = Brushes.Pink;
        private readonly Brush intermidiateBackground = new SolidColorBrush(Color.FromRgb(215, 244, 215));

        public SentencePracticeWindow(PracticeData practiceData, bool shuffle)
        {
            InitializeComponent();

            this.practiceData = practiceData;
            this.shuffle = shuffle;

            if (shuffle) this.practiceData.RemoveDuplicates();

            SpeedChart.AxisX[0].Separator.Step = 1;

            NextSentence();
        }

        private Brush MapDiffState(Differ.DiffData.DiffState state)
        {
            switch (state)
            {
                case Differ.DiffData.DiffState.Equal:
                    return correctBackground;
                case Differ.DiffData.DiffState.Unequal:
                    return incorrectBackground;
                case Differ.DiffData.DiffState.Intermediate:
                    return intermidiateBackground;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private void NextSentence()
        {
            if (CurrentTextBox.Text != "")
            {
                string previousSentence = CurrentTextBox.Text;
                TypingSpeed = Convert.ToInt32(typingMeasurer.Finish(previousSentence));

                TypingSpeedCollection[0].Values.Add(TypingSpeed);
            }

            if (shuffle)
            {
                if (currentSentenceIndex is null)
                {
                    currentSentenceIndex = SentenceIndexRandom.Next(practiceData.TextData.Count);
                }
                else
                {
                    int tempIndex = currentSentenceIndex.Value;
                    while ((currentSentenceIndex = SentenceIndexRandom.Next(practiceData.TextData.Count)) == tempIndex);
                }
            }
            else
            {
                if (currentSentenceIndex is null) currentSentenceIndex = 0;
                else if (currentSentenceIndex == practiceData.TextData.Count - 1) currentSentenceIndex = 0;
                else currentSentenceIndex++;
            }

            PreviousTextBlock.Inlines.Clear();
            var diffs = new List<Differ.DiffData>(CalculateDiff(CurrentText, CurrentTextBox.Text));
            for (int i = 0; i < diffs.Count(); i++)
            {
                if (diffs[i].State == Differ.DiffData.DiffState.Intermediate)
                {
                    diffs[i].State = Differ.DiffData.DiffState.Unequal;
                }
            }

            foreach (var diff in diffs)
            {
                var run = new Run(diff.Text)
                {
                    Background = MapDiffState(diff.State)
                };
                PreviousTextBlock.Inlines.Add(run);
            }

            string nextSentence = practiceData.TextData[currentSentenceIndex.Value];
            CurrentText = nextSentence;
        }

        private static IEnumerable<char> DecomposeHangul(char hangul)
        {
            var choseongTable = new List<string>
            {
                "ㄱ",
                "ㄱㄱ",
                "ㄴ",
                "ㄷ",
                "ㄷㄷ",
                "ㄹ",
                "ㅁ",
                "ㅂ",
                "ㅂㅂ",
                "ㅅ",
                "ㅅㅅ",
                "ㅇ",
                "ㅈ",
                "ㅈㅈ",
                "ㅊ",
                "ㅋ",
                "ㅌ",
                "ㅍ",
                "ㅎ",
            };
            var jungseongTable = new List<string>
            {
                "ㅏ",
                "ㅐ",
                "ㅑ",
                "ㅒ",
                "ㅓ",
                "ㅔ",
                "ㅕ",
                "ㅖ",
                "ㅗ",
                "ㅗㅏ",
                "ㅗㅐ",
                "ㅗㅣ",
                "ㅛ",
                "ㅜ",
                "ㅜㅓ",
                "ㅜㅔ",
                "ㅜㅣ",
                "ㅠ",
                "ㅡ",
                "ㅡㅣ",
                "ㅣ"
            };
            var jongseongTable = new List<string>
            {
                " ",
                "ㄱ",
                "ㄱㄱ",
                "ㄱㅅ",
                "ㄴ",
                "ㄴㅈ",
                "ㄴㅎ",
                "ㄷ",
                "ㄹ",
                "ㄹㄱ",
                "ㄹㅁ",
                "ㄹㅂ",
                "ㄹㅅ",
                "ㄹㅌ",
                "ㄹㅍ",
                "ㄹㅎ",
                "ㅁ",
                "ㅂ",
                "ㅂㅅ",
                "ㅅ",
                "ㅅㅅ",
                "ㅇ",
                "ㅈ",
                "ㅊ",
                "ㅋ",
                "ㅌ",
                "ㅍ",
                "ㅎ"
            };

            if (hangul >= (char)0x3131 && hangul <= (char)0x3163) // The character is in Hangul Compatibility Jamo unicode block
            {
                return new List<char> { hangul };
            }
            if (hangul < (char)0xAC00 || hangul > (char)0xD79F) // The character is not in Hangul Syllables unicode block
            {
                return new List<char>();
            }

            int code = hangul - (char)0xAC00;
            var result = new List<char>();

            int choseongIndex = code / (21 * 28);
            result.AddRange(choseongTable[choseongIndex]);
            code %= 21 * 28;

            int jungseongIndex = code / 28;
            result.AddRange(jungseongTable[jungseongIndex]);
            code %= 28;

            int jongseongIndex = code;
            if (jongseongIndex != 0) result.AddRange(jongseongTable[jongseongIndex]);

            return result;
        }

        private static IEnumerable<Differ.DiffData> CalculateDiff(string str1, string str2)
        {
            var differ = new Differ();

            Differ.DiffData.DiffState Comparer(char ch1, char ch2)
            {
                var decomposed1 = new List<char>(DecomposeHangul(ch1));
                var decomposed2 = new List<char>(DecomposeHangul(ch2));

                if (decomposed1.Any() && decomposed2.Any())
                {
                    if (decomposed1.SequenceEqual(decomposed2))
                    {
                        return Differ.DiffData.DiffState.Equal;
                    }

                    if (decomposed1.Count < decomposed2.Count)
                    {
                        return decomposed1.SequenceEqual(decomposed2.Take(decomposed1.Count))
                            ? Differ.DiffData.DiffState.Intermediate
                            : Differ.DiffData.DiffState.Unequal;
                    }

                    return decomposed2.SequenceEqual(decomposed1.Take(decomposed2.Count))
                        ? Differ.DiffData.DiffState.Intermediate
                        : Differ.DiffData.DiffState.Unequal;
                }

                return ch1 == ch2 ? Differ.DiffData.DiffState.Equal : Differ.DiffData.DiffState.Unequal;
            }

            var diffs
                = new List<Differ.DiffData>(differ.Diff(str1, str2, Comparer));

            return diffs;
        }

        private void CurrentTextBox_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                NextSentence();
                CurrentTextBox.Text = "";
                e.Handled = true;

                return;
            }
            if (CurrentTextBox.Text == "")
            {
                typingMeasurer.Start();
            }
        }

        private void CurrentTextBox_TextChanged(object sender, System.Windows.Controls.TextChangedEventArgs e)
        {
            string input = CurrentTextBox.Text;
            var diffs
                = new List<Differ.DiffData>(CalculateDiff(CurrentText.Substring(0, Math.Min(input.Length, CurrentText.Length)),
                                                          CurrentTextBox.Text));

            for (int i = 0; i < diffs.Count() - 1; i++)
            {
                if (diffs[i].State == Differ.DiffData.DiffState.Intermediate)
                {
                    diffs[i].State = Differ.DiffData.DiffState.Unequal;
                }
            }
            
            CurrentTextBlock.Inlines.Clear();
            foreach (Differ.DiffData diff in diffs)
            {
                var run = new Run(diff.Text)
                {
                    Background = MapDiffState(diff.State)
                };
                CurrentTextBlock.Inlines.Add(run);
            }

            if (input.Length < CurrentText.Length)
            {
                CurrentTextBlock.Inlines.Add(new Run(CurrentText.Substring(input.Length)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void OnPropertyChanged([CallerMemberName] string propertyName = null)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected bool SetField<T>(ref T field, T value, [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value)) return false;
            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }
    }
}
