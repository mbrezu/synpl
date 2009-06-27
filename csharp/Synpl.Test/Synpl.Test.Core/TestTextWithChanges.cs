using System;
using NUnit.Framework;
using Synpl.Core;
using System.Collections.Generic;

namespace Synpl.Test.Core
{
    [TestFixture]
    public class TestTextWithChanges
    {
        #region Private Storage
        private TextWithChanges _textWc;
        #endregion
        
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

        [Test]
        public void TestGetOldSlice()
        {
            SetupText();
            CowList<CharWithPosition> slice0 = _textWc.GetOldSlice(0, 20);
            CowList<CharWithPosition> expected = 
                new CowList<CharWithPosition>() { 
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
            CowList<CharWithPosition> slice0 = _textWc.GetCurrentSlice(0, 20);
            CowList<CharWithPosition> expected = 
                new CowList<CharWithPosition>() { 
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

        [Test]
        public void TestValidateSlice()
        {
            SetupText();
            _textWc.ValidateSlice(3, 8);
            Assert.AreEqual("_A_n_a_ _b_c_e_ _m_e_r_e_.", _textWc.TestRender());
        }

        [Test]
        public void TestValidateSliceAfterDeletingLastChar()
        {
            SetupText();
            _textWc.DeleteChar(_textWc.GetActualLength() - 1);
            Assert.AreEqual("_A_n_a_ AbAcDaDr_e_ _m_e_r_eD.",
                            _textWc.TestRender());
            _textWc.ValidateSlice(0, _textWc.GetActualLength());
            Assert.AreEqual("_A_n_a_ _b_c_e_ _m_e_r_e",
                            _textWc.TestRender());
        }

        [Test]
        public void TestValidateSlice2()
        {
            _textWc.SetText("a");
            _textWc.InsertChar(' ', 1);
            Assert.AreEqual("_aA ", _textWc.TestRender());
            _textWc.ValidateSlice(0, 1);
            Assert.AreEqual("_aA ", _textWc.TestRender());
        }

        [Test]
        public void TestGetSliceWithChanges()
        {
            SetupText();
            TextWithChanges slice0 = _textWc.GetSliceWithChanges(2, 9);
            Assert.AreEqual("_a_ AbAcDaDr_e_ _m", slice0.TestRender());
        }

        [Test]
        public void TestRemoveSliceWithChanges()
        {
            SetupText();
            _textWc.DeleteChar(11);
            _textWc.RemoveSliceWithChanges(2, 9);
            Assert.AreEqual("_A_n_e_rDe_.", _textWc.TestRender());
        }

        [Test]
        public void TestInsertSliceWithChanges()
        {
            SetupText();
            _textWc.DeleteChar(11);
            Assert.AreEqual("_A_n_a_ AbAcDaDr_e_ _m_e_rDe_.", _textWc.TestRender());
            TextWithChanges slice0 = _textWc.GetSliceWithChanges(2, 9);
            Assert.AreEqual("_a_ AbAcDaDr_e_ _m", slice0.TestRender());
            _textWc.RemoveSliceWithChanges(2, 9);
            Assert.AreEqual("_A_n_e_rDe_.", _textWc.TestRender());
            _textWc.InsertSliceWithChanges(2, slice0);
            Assert.AreEqual("_A_n_a_ AbAcDaDr_e_ _m_e_rDe_.", _textWc.TestRender());
        }

        #region Private Helper Methods
        // The following functions are debugging helpers, they are not
        // used normally.
        #pragma warning disable 0169
        private static void ShowChanges(TextWithChanges twc)
        {
            Console.WriteLine("\n***");
            foreach (TextChange change in twc.GetChanges())
            {
                Console.WriteLine("{0}, {1}", change.Position, change.IsDeletion);
            }
        }

        private void ShowParsableSlice(List<CharWithPosition> slice)
        {
            foreach (CharWithPosition cwp in slice)
            {
                Console.WriteLine("{0}", cwp);
            }
        }
        #endregion

    }
}
