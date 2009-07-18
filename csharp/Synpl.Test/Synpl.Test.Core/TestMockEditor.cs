// Synpl - a "structured editor" plugin for Gedit.
// Copyright (C) 2009  Miron Brezuleanu <mbrezu@gmail.com>
// 
// This program is free software; you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation; either version 2 of the License, or
// (at your option) any later version.
// 
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License along
// with this program; if not, write to the Free Software Foundation, Inc.,
// 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
// 

using System;
using Synpl.EditorAbstraction;
using System.Collections.Generic;
using NUnit.Framework;
using System.Text;

namespace Synpl.Test.Core
{
    [TestFixture]
    public class TestMockEditor
    {

        #region MockEditor Tests (meta-testing? :-p )
        [Test]
        public void TestSimulateInsertText()
        {
            MockEditor med = new MockEditor();
            Assert.AreEqual(String.Empty, med.GetText(0, med.Length));
            string testString = "Ana are mere.";
            med.SimulateInsertText(testString);
            Assert.AreEqual(testString, med.GetText(0, med.Length));
        }

        [Test]
        public void TestSimulateDeleteOneChar()
        {
            MockEditor med = SetupMockEditor("Ana are mere.");
            med.SimulateMoveRight(false);
            med.SimulateMoveRight(false);
            med.SimulateDelKeyStroke();
            string expectedContent = "An are mere.";
            Assert.AreEqual(expectedContent, med.GetText(0, med.Length));
        }

        [Test]
        public void TestSimulateDeleteSelection()
        {
            MockEditor med = SetupMockEditor("Ana are mere.");
            med.SimulateStartSelecting();
            med.SimulateMoveRight(true);
            med.SimulateMoveRight(true);
            med.SimulateDelKeyStroke();
            string expectedContent = "a are mere.";
            Assert.AreEqual(expectedContent, med.GetText(0, med.Length));
        }

        [Test]
        public void TestSimulateMoveUpDown()
        {
            MockEditor med = SetupMockEditor("Line 1.\nLine number two.\nThird line.");
            RepeatIt(3, delegate() { med.SimulateMoveRight(false); });
            RepeatIt(1, delegate() { med.SimulateMoveDown(false); });
            int line, column;
            med.OffsetToLineColumn(med.CursorOffset, out line, out column);
            Assert.AreEqual(1, line);
            Assert.AreEqual(3, column);
            RepeatIt(10, delegate() { med.SimulateMoveRight(false); });
            RepeatIt(1, delegate() { med.SimulateMoveDown(false); });
            med.OffsetToLineColumn(med.CursorOffset, out line, out column);
            Assert.AreEqual(2, line);
            Assert.AreEqual(11, column);
        }

        [Test]
        public void TestSimulateBackSpace()
        {
            MockEditor med = SetupMockEditor("Line 1.\nLine number two.\nThird line.");
            med.MoveForwardChars(8);
            med.SimulateBackspaceKeyStroke();
            Assert.AreEqual("Line 1.Line number two.\nThird line.", 
                            med.GetText(0, med.Length));
        }

        [Test]
        public void TestGetSelection()
        {
            MockEditor med = SetupMockEditor("Line 1.\nLine number two.\nThird line.");
            RepeatIt(9, delegate() { med.SimulateMoveRight(false); });
            RepeatIt(6, delegate() { med.SimulateMoveRight(true); });
            int selectionStart, selectionEnd;
            med.GetSelection(out selectionStart, out selectionEnd);
            Assert.AreEqual("ine nu", 
                            med.GetText(selectionStart, 
                                        selectionEnd - selectionStart));
        }

        [Test]
        public void TestSetSelection()
        {
            MockEditor med = SetupMockEditor("Line 1.\nLine number two.\nThird line.");
            med.SetSelection(0, 8);
            int selectionStart, selectionEnd;
            med.GetSelection(out selectionStart, out selectionEnd);
            Assert.AreEqual("Line 1.\n", 
                            med.GetText(selectionStart, 
                                        selectionEnd - selectionStart));
        }

        [Test]
        public void TestLength()
        {
            string testString = "Line 1.\nLine number two.\nThird line.";
            MockEditor med = SetupMockEditor(testString);
            Assert.AreEqual(testString.Length, med.Length);
        }

        [Test]
        public void TestLineColumnToOffset()
        {
            MockEditor med = SetupMockEditor("Line 1.\nLine number two.\nThird line.");
            med.CursorOffset = med.LineColumnToOffset(1, 0);
            Assert.AreEqual(8, med.CursorOffset);
            med.CursorOffset = med.LineColumnToOffset(1, 7);
            Assert.AreEqual(15, med.CursorOffset);
            med.CursorOffset = med.LineColumnToOffset(2, 3);
            Assert.AreEqual(28, med.CursorOffset);
            med.CursorOffset = med.LineColumnToOffset(1, 16);
            Assert.AreEqual(24, med.CursorOffset);
            med.CursorOffset = med.LineColumnToOffset(2, 0);
            Assert.AreEqual(25, med.CursorOffset);
       }

        [Test]
        public void TestOffsetToLineColumn()
        {
            MockEditor med = SetupMockEditor("Line 1.\nLine number two.\nThird line.");
            int testLine, testColumn;
            med.CursorOffset = 8;
            med.OffsetToLineColumn(med.CursorOffset, out testLine, out testColumn);
            Assert.AreEqual(1, testLine);
            Assert.AreEqual(0, testColumn);
            med.CursorOffset = 15;
            med.OffsetToLineColumn(med.CursorOffset, out testLine, out testColumn);
            Assert.AreEqual(1, testLine);
            Assert.AreEqual(7, testColumn);
            med.CursorOffset = 28;
            med.OffsetToLineColumn(med.CursorOffset, out testLine, out testColumn);
            Assert.AreEqual(2, testLine);
            Assert.AreEqual(3, testColumn);
            med.CursorOffset = 25;
            med.OffsetToLineColumn(med.CursorOffset, out testLine, out testColumn);
            Assert.AreEqual(2, testLine);
            Assert.AreEqual(0, testColumn);
            med.CursorOffset = 24;
            med.OffsetToLineColumn(med.CursorOffset, out testLine, out testColumn);
            Assert.AreEqual(1, testLine);
            Assert.AreEqual(16, testColumn);
        }
        
        [Test]
        public void TestInsertText()
        {
            MockEditor med = new MockEditor();
            bool shouldNotRaiseEvent = true;
            bool eventFired = false;
            med.TextChanged += 
                new EventHandler<TextChangedEventArgs>(delegate(object sender, TextChangedEventArgs e) {
                    Assert.IsFalse(shouldNotRaiseEvent);
                    Assert.AreEqual(3, e.Length);
                    Assert.AreEqual("Kat", e.Text);
                    Assert.AreEqual(0, e.Start);
                    Assert.AreEqual(TextChangedEventArgs.OperationType.Insertion,
                                    e.Operation);
                    eventFired = true;
                });
            med.InsertText(0, "Ana are mere.", true);
            shouldNotRaiseEvent = false;
            Assert.IsFalse(eventFired);
            med.InsertText(0, "Kat", false);
            Assert.IsTrue(eventFired);
            Assert.AreEqual("KatAna are mere.", med.GetText(0, med.Length));
        }

        [Test]
        public void TestDeleteText()
        {
            MockEditor med = new MockEditor();
            bool shouldNotRaiseEvent = true;
            bool eventFired = false;
            med.TextChanged += 
                new EventHandler<TextChangedEventArgs>(delegate(object sender, TextChangedEventArgs e) {
                    Assert.IsFalse(shouldNotRaiseEvent);
                    Assert.AreEqual(4, e.Length);
                    Assert.AreEqual("are ", e.Text);
                    Assert.AreEqual(4, e.Start);
                    Assert.AreEqual(TextChangedEventArgs.OperationType.Deletion,
                                    e.Operation);
                    eventFired = true;
                });
            med.InsertText(0, "Ana are mere.", true);
            shouldNotRaiseEvent = false;
            Assert.IsFalse(eventFired);
            med.DeleteText(4, 4, false);
            Assert.IsTrue(eventFired);
            Assert.AreEqual("Ana mere.", med.GetText(0, med.Length));
        }

        [Test]
        public void TestGetText()
        {
            MockEditor med = SetupMockEditor("Ana are mere.");
            Assert.AreEqual("mer", med.GetText(8, 3));
        }
        
        [Test]
        public void TestMoveToStartOfLine()
        {
            MockEditor med = SetupMockEditor("Line 1.\nLine number two.\nThird line.");
            med.MoveForwardLines(1);
            med.MoveForwardChars(6);
            med.MoveToStartOfLine();
            Assert.AreEqual(8, med.CursorOffset);
            int testLine, testColumn;
            med.OffsetToLineColumn(med.CursorOffset, out testLine, out testColumn);
            Assert.AreEqual(1, testLine);
            Assert.AreEqual(0, testColumn);
        }

        [Test]
        public void TestMoveToEndOfLine()
        {
            MockEditor med = SetupMockEditor("Line 1.\nLine number two.\nThird line.");
            med.MoveForwardLines(1);
            med.MoveForwardChars(6);
            med.MoveToEndOfLine();
            Assert.AreEqual(24, med.CursorOffset);
            int testLine, testColumn;
            med.OffsetToLineColumn(med.CursorOffset, out testLine, out testColumn);
            Assert.AreEqual(1, testLine);
            Assert.AreEqual(16, testColumn);
        }

        [Test]
        public void TestLastColumnOnLine()
        {
            MockEditor med = SetupMockEditor("Line 1.\nLine number two.\nThird line.");            
            Assert.AreEqual(16, med.LastColumnOnLine(1));            
        }

        [Test]
        public void TestOffsetStartLine()
        {
            MockEditor med = SetupMockEditor("Line 1.\nLine number two.\nThird line.");
            Assert.AreEqual(8, med.OffsetStartLine(1));
            Assert.AreEqual(25, med.OffsetStartLine(2));
        }

        #endregion

        #region Private Helper Methods
        private void RepeatIt(int times, Action action)
        {
            for (int i = 0; i < times; i++)
            {
                action();
            }
        }

        private MockEditor SetupMockEditor(string text)
        {
            MockEditor result = new MockEditor();
            result.SimulateInsertText(text);
            result.CursorOffset = 0;
            return result;
        }
        #endregion

    }
}
