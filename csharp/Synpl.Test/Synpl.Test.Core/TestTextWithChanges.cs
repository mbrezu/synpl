using System;
using NUnit.Framework;
using Synpl.Core;
using System.Collections.Generic;

namespace Synpl.Test.Core
{
    [TestFixture]
    public class Tests
    {
        private TextWithChanges _textWc;
        [SetUp]
        public void Init()
        {
            _textWc = new TextWithChanges();
            _textWc.SetText("Ana re mere.");
        }
        
        [Test]
        public void TestInsertion()
        {
            _textWc.InsertChar('a', 4);
            Assert.AreEqual(1,
                            _textWc.GetChanges().Count);
            Assert.AreEqual("_A_n_a_ Aa_r_e_ _m_e_r_e_.",
                            _textWc.TestRender());
        }

        [Test]
        public void TestDeletion()
        {
            _textWc.SetText("Ana are mere.");
            _textWc.DeleteChar(4);
            Assert.AreEqual(1,
                            _textWc.GetChanges().Count);
            Assert.AreEqual("_A_n_a_ Da_r_e_ _m_e_r_e_.",
                            _textWc.TestRender());
        }

        private void SetupText()
        {
            _textWc.SetText("Ana are mere.");
            _textWc.DeleteChar(4);
            _textWc.DeleteChar(4);
//            Console.WriteLine(_textWc.TestRender());
            _textWc.InsertChar('b', 4);
            // Should try inserting c at 4 as a separate scenario.
            _textWc.InsertChar('c', 5);
//            Console.WriteLine(_textWc.TestRender());
        }

//        private void ShowChanges()
//        {
//            foreach (TextChange change in _textWc.GetChanges())
//            {
//                Console.WriteLine("{0}, {1}", change.Position, change.IsDeletion);
//            }
//        }

        private void ShowParsableSlice(List<CharWithPosition> slice)
        {
            foreach (CharWithPosition cwp in slice)
            {
                Console.WriteLine("{0}", cwp);
            }
        }

        [Test]
        public void TestGetOldSlice()
        {
            SetupText();
            List<CharWithPosition> slice0 = _textWc.GetOldSlice(0, 20);
            List<CharWithPosition> expected = 
                new List<CharWithPosition>() { 
                new CharWithPosition(0, 'A'),
                new CharWithPosition(1, 'n'),
                new CharWithPosition(2, 'a'),
                new CharWithPosition(3, ' '),
                new CharWithPosition(6, 'a'),
                new CharWithPosition(6, 'r'),
                new CharWithPosition(6, 'e'),
                new CharWithPosition(7, ' '),
                new CharWithPosition(8, 'm'),
                new CharWithPosition(9, 'e'),
                new CharWithPosition(10, 'r'),
                new CharWithPosition(11, 'e'),
                new CharWithPosition(12, '.')
            };
            Assert.AreEqual(expected, slice0);
        }

        [Test]
        public void TestGetCurrentSlice()
        {
            SetupText();
            List<CharWithPosition> slice0 = _textWc.GetCurrentSlice(0, 20);
            List<CharWithPosition> expected = 
                new List<CharWithPosition>() { 
                new CharWithPosition(0, 'A'),
                new CharWithPosition(1, 'n'),
                new CharWithPosition(2, 'a'),
                new CharWithPosition(3, ' '),
                new CharWithPosition(4, 'b'),
                new CharWithPosition(5, 'c'),
                new CharWithPosition(6, 'e'),
                new CharWithPosition(7, ' '),
                new CharWithPosition(8, 'm'),
                new CharWithPosition(9, 'e'),
                new CharWithPosition(10, 'r'),
                new CharWithPosition(11, 'e'),
                new CharWithPosition(12, '.')
            };
            Assert.AreEqual(expected, slice0);
        }

    }
}
