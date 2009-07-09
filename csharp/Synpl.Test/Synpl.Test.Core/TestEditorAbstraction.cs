// // Synpl - a "structured editor" plugin for Gedit.
// // Copyright (C) 2009  Miron Brezuleanu <mbrezu@gmail.com>
// 
// // This program is free software; you can redistribute it and/or modify
// // it under the terms of the GNU General Public License as published by
// // the Free Software Foundation; either version 2 of the License, or
// // (at your option) any later version.
// 
// // This program is distributed in the hope that it will be useful,
// // but WITHOUT ANY WARRANTY; without even the implied warranty of
// // MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// // GNU General Public License for more details.
// 
// // You should have received a copy of the GNU General Public License along
// // with this program; if not, write to the Free Software Foundation, Inc.,
// // 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA.
// 

using System;
using Synpl.EditorAbstraction;
using System.Collections.Generic;
using NUnit.Framework;
using System.Text;

namespace Synpl.Test.Core
{
    
    [TestFixture]
    public class TestEditorAbstraction
    {
        
        public TestEditorAbstraction()
        {
        }

        [Test]
        public void TestKeyConstructor()
        {
            Key key = new Key("A", false, true, false);
            Assert.AreEqual("A", key.KeyCode);
            Assert.AreEqual(false, key.Shift);
            Assert.AreEqual(true, key.Control);
            Assert.AreEqual(false, key.Alt);

            Key key2 = new Key("C-a");
            Assert.AreEqual(key, key2);
        }

        [Test]
        public void TestKeyToString()
        {
            Key key = new Key("C-a");
            Assert.AreEqual("C-a", key.ToString());
        }
        
        [Test]
        public void TestKeyEquality()
        {
            Key key1 = new Key("C-a");
            Key key2 = new Key("C-f10");
            Key key3 = new Key("C-f10");

            Assert.AreNotEqual(key1, key2);
            Assert.AreEqual(key2, key3);
        }
    }
}
