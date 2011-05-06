/* Copyright (C) 2011 Karoly Lukacs <lukamicoder@gmail.com>
 *
 * Based on code created by Mozilla Labs.
 * 
 * This is free software; you can redistribute it and/or
 * modify it under the terms of the GNU Library General Public
 * License as published by the Free Software Foundation; either
 * version 3 of the License, or (at your option) any later version.
 *
 * This library is distributed in the hope that it will be useful,
 * but WITHOUT ANY WARRANTY; without even the implied warranty of
 * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
 * Library General Public License for more details.
 *
 * You should have received a copy of the GNU Library General Public
 * License along with this software; if not, write to the
 * Free Software Foundation, Inc., 59 Temple Place - Suite 330,
 * Boston, MA 02111-1307, USA.
 */

namespace WeaveCore {
    public abstract class WeaveLogEventBase {
        public delegate void WeaveLogEventHandler(object sender, WeaveLogEventArgs e);
        public event WeaveLogEventHandler LogEvent;

        protected void RaiseLogEvent(object source, string msg, LogType type) {
            OnLogEvent(source, new WeaveLogEventArgs(msg, type));
        }

        protected void OnLogEvent(object source, WeaveLogEventArgs args) {
            WeaveLogEventHandler tmp = LogEvent;
            if (tmp != null) {
                tmp(source, args);
            }
        }
    }
}
