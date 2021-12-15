namespace ReplaysToCSV
{
	public class SubStream : Stream
	{
		private readonly long length;
		private readonly Stream baseStream;
		private long position;

		public SubStream(Stream baseStream, long offset, long length)
		{
			if (!baseStream.CanRead)
			{
				throw new ArgumentException("can't read base stream");
			}

			if (offset < 0)
			{
				throw new ArgumentOutOfRangeException(nameof(offset));
			}

			this.baseStream = baseStream;
			this.length = length;

			if (baseStream.CanSeek)
			{
				baseStream.Seek(offset, SeekOrigin.Current);
			}
			else
			{ // read it manually...
				const int BUFFER_SIZE = 512;
				byte[] buffer = new byte[BUFFER_SIZE];
				while (offset > 0)
				{
					int read = baseStream.Read(buffer, 0, offset < BUFFER_SIZE ? (int)offset : BUFFER_SIZE);
					offset -= read;
				}
			}
		}

		public override long Length
		{
			get
			{
				CheckDisposed();
				return length;
			}
		}

		public override bool CanRead
		{
			get
			{
				CheckDisposed();
				return true;
			}
		}

		public override bool CanWrite
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		public override bool CanSeek
		{
			get
			{
				CheckDisposed();
				return false;
			}
		}

		public override long Position
		{
			get
			{
				CheckDisposed();
				return position;
			}

			set
			{
				throw new NotSupportedException();
			}
		}

		public override int Read(byte[] buffer, int offset, int count)
		{
			CheckDisposed();
			long remaining = length - position;
			if (remaining <= 0)
			{
				return 0;
			}

			if (remaining < count)
			{
				count = (int)remaining;
			}

			int read = baseStream.Read(buffer, offset, count);
			position += read;
			return read;
		}

		public override long Seek(long offset, SeekOrigin origin)
		{
			throw new NotSupportedException();
		}

		public override void SetLength(long value)
		{
			throw new NotSupportedException();
		}

		public override void Flush()
		{
			CheckDisposed();
			baseStream.Flush();
		}

		public override void Write(byte[] buffer, int offset, int count)
		{
			throw new NotImplementedException();
		}

		protected override void Dispose(bool disposing)
		{
			base.Dispose(disposing);
			if (disposing)
			{
				if (baseStream != null)
				{
					try
					{
						baseStream.Dispose();
					}
					catch
					{
					}

					// baseStream = null;
				}
			}
		}

		private void CheckDisposed()
		{
			if (baseStream == null)
			{
				throw new ObjectDisposedException(GetType().Name);
			}
		}
	}
}
