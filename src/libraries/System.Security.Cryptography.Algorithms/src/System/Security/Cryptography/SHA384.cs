// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using Internal.Cryptography;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;

namespace System.Security.Cryptography
{
    //
    // If you change anything in this class, you must make the same change in the other *Provider classes. This is a pain but given that the
    // preexisting contract from the .NET Framework locks all of these into deriving directly from the abstract HashAlgorithm class,
    // it can't be helped.
    //

    public abstract class SHA384 : HashAlgorithm
    {
        private const int HashSizeBits = 384;
        private const int HashSizeBytes = HashSizeBits / 8;

        protected SHA384()
        {
            HashSizeValue = HashSizeBits;
        }

        public static new SHA384 Create() => new Implementation();

        public static new SHA384? Create(string hashName) => (SHA384?)CryptoConfig.CreateFromName(hashName);

        /// <summary>
        /// Computes the hash of data using the SHA384 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static byte[] HashData(byte[] source)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            return HashData(new ReadOnlySpan<byte>(source));
        }

        /// <summary>
        /// Async Computes the hash of data using the SHA384 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="cancellationToken">CancellationToken used to cancel the hash operation.</param>
        /// <returns>The hash of the data.</returns>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        public static async Task<byte[]> HashDataAsync(byte[] source, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            // Note to self:  Verify if this will leak or not.
            byte[] buffer = GC.AllocateUninitializedArray<byte>(HashSizeBytes);

            int written = await HashDataAsync(source, buffer, cancellationToken).ConfigureAwait(false);

            return buffer;
        }

        /// <summary>
        /// Async Computes the hash of data using the SHA384 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        /// <param name="cancellationToken">CancellationToken used to cancel the hash operation.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">
        /// The buffer in <paramref name="destination"/> is too small to hold the calculated hash
        /// size. The SHA384 algorithm always produces a 384-bit hash, or 48 bytes.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="source" /> is <see langword="null" />.
        /// </exception>
        /// <exception cref="ArgumentNullException">
        /// <paramref name="destination" /> is <see langword="null" />.
        /// </exception>
        public static async Task<int> HashDataAsync(byte[] source, byte[] destination, CancellationToken cancellationToken = default)
        {
            if (source is null)
                throw new ArgumentNullException(nameof(source));

            if (destination is null)
                throw new ArgumentNullException(nameof(destination));

            (bool IsSuccess, int BytesWritten) hashResult = await TryHashDataAsync(source, destination, cancellationToken).ConfigureAwait(false);
            if (!hashResult.IsSuccess)
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));
            Debug.Assert(hashResult.BytesWritten == destination.Length);
            return hashResult.BytesWritten;
        }

        /// <summary>
        /// Computes the hash of data using the SHA384 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <returns>The hash of the data.</returns>
        public static byte[] HashData(ReadOnlySpan<byte> source)
        {
            byte[] buffer = GC.AllocateUninitializedArray<byte>(HashSizeBytes);

            int written = HashData(source, buffer.AsSpan());
            Debug.Assert(written == buffer.Length);

            return buffer;
        }

        /// <summary>
        /// Computes the hash of data using the SHA384 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        /// <returns>The total number of bytes written to <paramref name="destination" />.</returns>
        /// <exception cref="ArgumentException">
        /// The buffer in <paramref name="destination"/> is too small to hold the calculated hash
        /// size. The SHA384 algorithm always produces a 384-bit hash, or 48 bytes.
        /// </exception>
        public static int HashData(ReadOnlySpan<byte> source, Span<byte> destination)
        {
            if (!TryHashData(source, destination, out int bytesWritten))
                throw new ArgumentException(SR.Argument_DestinationTooShort, nameof(destination));

            return bytesWritten;
        }

        /// <summary>
        /// Attempts to compute the hash of data using the SHA384 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        /// <param name="bytesWritten">
        /// When this method returns, the total number of bytes written into <paramref name="destination"/>.
        /// </param>
        /// <returns>
        /// <see langword="false"/> if <paramref name="destination"/> is too small to hold the
        /// calculated hash, <see langword="true"/> otherwise.
        /// </returns>
        public static bool TryHashData(ReadOnlySpan<byte> source, Span<byte> destination, out int bytesWritten)
        {
            if (destination.Length < HashSizeBytes)
            {
                bytesWritten = 0;
                return false;
            }

            bytesWritten = HashProviderDispenser.OneShotHashProvider.HashData(HashAlgorithmNames.SHA384, source, destination);
            Debug.Assert(bytesWritten == HashSizeBytes);

            return true;
        }

        /// <summary>
        /// Attempts to compute the hash of data using the SHA1 algorithm.
        /// </summary>
        /// <param name="source">The data to hash.</param>
        /// <param name="destination">The buffer to receive the hash value.</param>
        /// <param name="cancellationToken">CancellationToken used to cancel the hash operation.</param>
        /// <returns>
        /// <see langword="false"/> if <paramref name="destination"/> is too small to hold the
        /// calculated hash, <see langword="true"/> otherwise.
        /// </returns>
        public static async Task<(bool IsSuccess, int BytesWritten)> TryHashDataAsync(byte[] source, byte[] destination, CancellationToken cancellationToken = default)
        {
            if (destination.Length < HashSizeBytes)
            {
                return (false, 0);
            }

            int bytesWritten = await HashProviderDispenser.OneShotHashProvider.HashDataAsync(HashAlgorithmNames.SHA384, source, destination, cancellationToken).ConfigureAwait(false);
            Debug.Assert(bytesWritten == HashSizeBytes);

            return (true, bytesWritten);
        }

        private sealed class Implementation : SHA384
        {
            private readonly HashProvider _hashProvider;

            public Implementation()
            {
                _hashProvider = HashProviderDispenser.CreateHashProvider(HashAlgorithmNames.SHA384);
                HashSizeValue = _hashProvider.HashSizeInBytes * 8;
            }

            protected sealed override void HashCore(byte[] array, int ibStart, int cbSize) =>
                _hashProvider.AppendHashData(array, ibStart, cbSize);

            protected sealed override void HashCore(ReadOnlySpan<byte> source) =>
                _hashProvider.AppendHashData(source);
            protected sealed override Task HashCoreAsync(byte[] array, int ibStart, int cbSize, CancellationToken cancellationToken) =>
                _hashProvider.AppendHashDataAsync(array, ibStart, cbSize, cancellationToken);
            protected sealed override byte[] HashFinal() =>
                _hashProvider.FinalizeHashAndReset();

            protected sealed override Task<byte[]> HashFinalAsync(CancellationToken cancellationToken) => _hashProvider.FinalizeHashAndResetAsync(cancellationToken);

            protected sealed override bool TryHashFinal(Span<byte> destination, out int bytesWritten) =>
                _hashProvider.TryFinalizeHashAndReset(destination, out bytesWritten);

            public sealed override void Initialize()
            {
                // Nothing to do here. We expect HashAlgorithm to invoke HashFinal() and Initialize() as a pair. This reflects the
                // reality that our native crypto providers (e.g. CNG) expose hash finalization and object reinitialization as an atomic operation.
            }

            protected sealed override void Dispose(bool disposing)
            {
                _hashProvider.Dispose(disposing);
                base.Dispose(disposing);
            }
        }
    }
}
