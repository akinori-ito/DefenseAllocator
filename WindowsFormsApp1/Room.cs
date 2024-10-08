﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace SlotAlignmentOptimizer
{
    // 部屋のクラス
    // 部屋は複数の時間スロットからなり、１つのスロットはイベントに対応する
    public class Room
    {
        public string name { get; private set; }
        public Event[] events { get; private set; }
        public int max_events { get; private set; }
        int tail;
        public bool changable { get; private set; }
        public int online { get; private set; }
        public Room(string name, int max)
        {
            this.name = name;
            max_events = max;
            events = new Event[max];
            for (int i = 0; i < max; i++)
            {
                events[i] = Event.NullEvent;
            }
            tail = 0;
            changable = true;
            if (name.StartsWith("オンライン"))
            {
                online = 1;
            } else
            {
                online = 0;
            }
        }
        public void addEvent(Event e)
        {
            while (tail < max_events && events[tail] != Event.NullEvent)
	        tail++;
            if (tail == max_events)
            {
                for (int i = 0; i < max_events; i++)
                    Console.WriteLine(i.ToString()+": "+events[i].ToString());
                throw new Exception();
            }
            events[tail] = e;
            tail++;
        }
        public void addEvent(Event e, int slot)
        {
            if (slot < 0 || max_events <= slot || events[slot] != Event.NullEvent)
            {
                throw new Exception();
            }
            events[slot] = e;
        }
        public Event Get(int i)
        {
            if (i >= max_events || events[i] == null)
            {
                return Event.NullEvent;
            }
            return events[i];
        }
        public void Set(int i, Event e)
        {
            events[i] = e;
        }
        public void unchangable()
        {
            changable = false;
        }
        public override string ToString()
        {
            string str = "<room name=\"" + name + "\" changable=\""+changable.ToString()+"\">";
            foreach (Event e in events)
            {
                str += e.ToString();
            }
            return str + "</room>";
        }
    }
}
