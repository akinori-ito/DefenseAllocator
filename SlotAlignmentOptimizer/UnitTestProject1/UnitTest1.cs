using Microsoft.VisualStudio.TestTools.UnitTesting;
using SlotAlignmentOptimizer;
using System;
using System.IO;

namespace UnitTestProject1
{
    [TestClass]
    public class UnitTest1
    {
        [TestMethod]
        public void TestMethod1()
        {
            var e = new Attendee("参加者1");
            Assert.AreEqual(e.getName(), "参加者1");
            Assert.AreEqual(e.getId(), 0);
            var e2 = new Attendee("参加者2");
            Assert.AreEqual(e2.getName(), "参加者2");
            Assert.AreEqual(e2.getId(), 1);
            var e3 = e;
            Assert.AreEqual(e.equals(e3), true);
            Assert.AreEqual(e.equals(e2), false);
        }
        [TestMethod]
        public void TestMethod2()
        {
            var e = new Event("イベント");
            Assert.AreEqual(e.name, "イベント");
            Assert.AreEqual(e.numberOfAttendees(), 0);
            var pool = new AttendeePool();
            e.Attendees.Add(pool.Get("参加者1"));
            e.Attendees.Add(pool.Get("参加者2"));
            e.Attendees.Add(pool.Get("参加者3"));
            Assert.AreEqual(e.numberOfAttendees(), 3);
            var e2 = new Event("イベント2");
            e2.Attendees.Add(pool.Get("参加者3"));
            e2.Attendees.Add(pool.Get("参加者2"));
            e2.Attendees.Add(pool.Get("参加者4"));
            Assert.AreEqual(e.overlap(e2), 2);
            Console.WriteLine(e.ToString());
            Console.WriteLine(e2.ToString());
        }
        [TestMethod]
        public void TestMethod3()
        {
            var r = new Room("Room1", 10);
            for (int i = 0; i < 10; i++)
            {
                r.addEvent(new Event("foo"));
            }
            for (int i = 0; i < 10; i++)
            {
                Assert.AreEqual(r.Get(i).name, "foo");
            }
            Assert.AreEqual(r.Get(11), Event.NullEvent);
            Assert.ThrowsException<Exception>(() => {
                r.addEvent(Event.NullEvent);
            });
        }
        [TestMethod]
        public void TestMethod4()
        {
            var rs = new Rooms(10);
            var rnd = new Random();
            var pool = new AttendeePool();
            for (int i = 0; i < 5; i++)
            {
                Room r = rs.AddRoom("Room" + i.ToString());
                for (int t = 0; t < 5; t++)
                {
                    Event e = new Event("event" + i.ToString() + "_" + t.ToString());
                    r.addEvent(e);
                    for (int j = 0; j < 3; j++)
                    {
                        e.Attendees.Add(pool.Get("Attendee" + rnd.Next(10).ToString()));
                    }
                }
            }
            Action<int, int, double, int> callback = (int epoch, int iter, double lossval, int doublecount) =>
            {
                StreamWriter fs = File.AppendText(@"C:\Users\user\Desktop\result.txt");
                fs.Write(epoch);
                fs.Write("\t");
                fs.Write(iter);
                fs.Write("\t");
                fs.WriteLine(lossval);
                fs.Close();
            };
            rs.anneal(50.0, 30, 1000, 1.2, pool.maxid+1, callback);
        }
    }
}
