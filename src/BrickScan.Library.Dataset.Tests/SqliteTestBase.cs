#region License
// Copyright (c) 2020 Jens Eisenbach
//
// Permission is hereby granted, free of charge, to any person
// obtaining a copy of this software and associated documentation
// files (the "Software"), to deal in the Software without
// restriction, including without limitation the rights to use,
// copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the
// Software is furnished to do so, subject to the following
// conditions:
//
// The above copyright notice and this permission notice shall be
// included in all copies or substantial portions of the Software.
//
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND,
// EXPRESS OR IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES
// OF MERCHANTABILITY, FITNESS FOR A PARTICULAR PURPOSE AND
// NONINFRINGEMENT. IN NO EVENT SHALL THE AUTHORS OR COPYRIGHT
// HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER LIABILITY,
// WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING
// FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR
// OTHER DEALINGS IN THE SOFTWARE.
#endregion

using System;
using BrickScan.Library.Dataset.Model;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace BrickScan.Library.Dataset.Tests
{
    public abstract class SqliteTestBase : IDisposable
    {                
        private const string IN_MEMORY_CONNECTION_STRING = "DataSource=:memory:";
        private readonly SqliteConnection _connection;
        private bool _isDisposed;

        protected SqliteTestBase()
        {
            _connection = new SqliteConnection(IN_MEMORY_CONNECTION_STRING);           
            _connection.Open();
        }

        ~SqliteTestBase()
        {
            Dispose(false);
        }

        protected DatasetDbContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<DatasetDbContext>()
                .UseSqlite(_connection)
                .Options;

            var context = new DatasetDbContext(options);
            context.Database.EnsureCreated();
            return context;
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }

            if (disposing)
            {
                _connection.Close();
            }

            _isDisposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
