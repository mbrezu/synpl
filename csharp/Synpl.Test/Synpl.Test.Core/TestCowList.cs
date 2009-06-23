
using System;
using NUnit.Framework;
using Synpl.Core;
using System.Collections.Generic;

namespace Synpl.Test.Core
{
    [TestFixture]
    public class TestCowList
    {

        private CowList<int> _cowList1;
        private CowList<string> _cowList2;
        
        [SetUp]
        public void Setup()
        {
            _cowList1 = new CowList<int>();
            _cowList1.Add(3);
            _cowList1.Add(4);
            _cowList1.Add(2);
            List<string> storage = new List<string>();
            storage.Add("Ana");
            storage.Add("are");
            storage.Add("mere");
            storage.Add(".");
            _cowList2 = new CowList<string>(storage);
        }
        
        [Test]
        public void TestHeadTail()
        {
            Assert.AreEqual(3, _cowList1.Head);
            CowList<int> tail = _cowList1.Tail;
            Assert.AreEqual(2, tail.Count);
            Assert.AreEqual(4, tail[0]);
            Assert.AreEqual(2, tail[1]);
        }

        [Test]
        public void TestCopy()
        {
            CowList<int> tail = _cowList1.Tail;
            tail[0] = 1;
            Assert.AreEqual(2, tail[1]);
            Assert.AreEqual(4, _cowList1[1]);
        }

        [Test]
        public void TestIndexOf()
        {
            Assert.AreEqual(1, _cowList2.IndexOf("are"));
            Assert.AreEqual(-1, _cowList2.IndexOf("John"));
            Assert.AreEqual(2, _cowList1.IndexOf(2));
            _cowList1.Add(-3);
            Assert.AreEqual(3, _cowList1.IndexOf(-3));
        }

        [Test]
        public void TestRemove()
        {
            Assert.AreEqual(true, _cowList1.Remove(2));
            Assert.AreEqual(2, _cowList1.Count);
            Assert.AreEqual(3, _cowList1[0]);
            Assert.AreEqual(4, _cowList1[1]);
            CowList<string> tail = _cowList2.Tail;
            tail.RemoveAt(1);
            Assert.AreEqual("are", tail[0]);
            Assert.AreEqual(".", tail[1]);
        }

        [Test]
        public void TestInsert()
        {
            CowList<int> tail = _cowList1.Tail;
            tail.Insert(0, 1);
            tail.Insert(2, 4);
            Assert.AreEqual(4, tail.Count);
            Assert.AreEqual(1, tail[0]);
            Assert.AreEqual(4, tail[1]);
            Assert.AreEqual(4, tail[2]);
            Assert.AreEqual(2, tail[3]);
        }

        [Test]
        public void TestAdd()
        {
            _cowList1.Add(4);
            _cowList1.Add(-3);
            Assert.AreEqual(5, _cowList1.Count);
            Assert.AreEqual(3, _cowList1[0]);
            Assert.AreEqual(4, _cowList1[1]);
            Assert.AreEqual(2, _cowList1[2]);
            Assert.AreEqual(4, _cowList1[3]);
            Assert.AreEqual(-3, _cowList1[4]);
        }

        [Test]
        public void TestSort()
        {
            _cowList1.Add(4);
            _cowList1.Add(-3);
            _cowList1.Sort();
            Assert.AreEqual(5, _cowList1.Count);
            Assert.AreEqual(-3, _cowList1[0]);
            Assert.AreEqual(2, _cowList1[1]);
            Assert.AreEqual(3, _cowList1[2]);
            Assert.AreEqual(4, _cowList1[3]);
            Assert.AreEqual(4, _cowList1[4]);
        }

        [Test]
        public void TestEnumerator()
        {
            int j = 0;
            foreach (int i in _cowList1)
            {
                switch (j)
                {
                case 0:
                    Assert.AreEqual(3, i);
                    break;
                case 1:
                    Assert.AreEqual(4, i);
                    break;
                case 2:
                    Assert.AreEqual(2, i);
                    break;
                default:
                    Assert.IsTrue(false);
                    break;
                }
                j++;
            }
        }

        [Test]
        public void TestEquals()
        {
            CowList<int> equalList = new CowList<int>();
            equalList.Add(3);
            equalList.Add(4);
            equalList.Add(2);
            Assert.AreEqual(equalList, _cowList1);
        }
    }
}
