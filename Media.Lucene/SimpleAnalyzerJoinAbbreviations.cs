using System;
using System.Collections.Generic;
using System.Linq;
using Lucene.Net.Analysis;
using Lucene.Net.Analysis.Tokenattributes;
using Lucene.Net.Util;


namespace MediaHub
{
    public class CharJoinAbbrLowerCaseExactAnalyzer : Analyzer
    {
        public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
        {
            TokenStream result = (TokenStream)new CharJoinAbbreviationsLowerCaseExactTokenizer(reader);

            return result;
        }

        public override TokenStream ReusableTokenStream(System.String fieldName, System.IO.TextReader reader)
        {
            SavedStreams streams = (SavedStreams)PreviousTokenStream;
            if (streams == null)
            {
                streams = new SavedStreams(this);
                streams.source = new CharJoinAbbreviationsLowerCaseExactTokenizer(reader);
                streams.result = streams.source; //if we dont have a filter above
                PreviousTokenStream = streams;
            }
            else
                streams.source.Reset(reader);
            return streams.result;
        }

        private class SavedStreams
        {
            public SavedStreams(CharJoinAbbrLowerCaseExactAnalyzer enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }
            private void InitBlock(CharJoinAbbrLowerCaseExactAnalyzer enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private CharJoinAbbrLowerCaseExactAnalyzer enclosingInstance;
            public CharJoinAbbrLowerCaseExactAnalyzer Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }
            internal Tokenizer source;
            internal TokenStream result;
        }
    }

    public class CharJoinAbbreviationsLowerCaseExactTokenizer : Tokenizer
    {
        public CharJoinAbbreviationsLowerCaseExactTokenizer(System.IO.TextReader input)
            : base(input)
        {
            offsetAtt = AddAttribute<IOffsetAttribute>();
            termAtt = AddAttribute<ITermAttribute>();
        }

        public CharJoinAbbreviationsLowerCaseExactTokenizer(AttributeSource source, System.IO.TextReader input)
            : base(source, input)
        {
            offsetAtt = AddAttribute<IOffsetAttribute>();
            termAtt = AddAttribute<ITermAttribute>();
        }

        public CharJoinAbbreviationsLowerCaseExactTokenizer(AttributeFactory factory, System.IO.TextReader input)
            : base(factory, input)
        {
            offsetAtt = AddAttribute<IOffsetAttribute>();
            termAtt = AddAttribute<ITermAttribute>();
        }

        private int offset = 0, bufferIndex = 0, dataLen = 0;
        private const int MAX_WORD_LEN = 255;
        private const int IO_BUFFER_SIZE = 4096;
        private char[] ioBuffer = new char[IO_BUFFER_SIZE];

        private ITermAttribute termAtt;
        private IOffsetAttribute offsetAtt;

        protected virtual bool IsTokenChar(char c)
        {
            return System.Char.IsLetterOrDigit(c) || IsTokenCharButExcluded(c);
        }

        protected virtual bool IsTokenCharButExcluded(char c)
        {
            return c == '.' || c == '\'';
        }

        protected virtual char Normalize(char c)
        {
            return System.Char.ToLower(c);
        }

        public override bool IncrementToken()
        {
            ClearAttributes();
            bool space = false;
            int length = 0, excluded = 0;
            int start = bufferIndex;
            char[] buffer = termAtt.TermBuffer();
            while (true)
            {
                if (bufferIndex >= dataLen)
                {
                    offset += dataLen;
                    dataLen = input.Read((System.Char[])ioBuffer, 0, ioBuffer.Length);
                    if (dataLen <= 0)
                    {
                        dataLen = 0; // so next offset += dataLen won't decrement offset
                        if (length > 0)
                            break;
                        else
                            return false;
                    }
                    bufferIndex = 0;
                }

                char c = ioBuffer[bufferIndex++];

                if (IsTokenChar(c))
                {
                    // if it's a token char

                    if (IsTokenCharButExcluded(c))
                    {
                        excluded++;
                        continue;
                    }

                    if (space == true)
                    {
                        if (length == buffer.Length)
                            buffer = termAtt.ResizeTermBuffer(1 + length);

                        buffer[length++] = ' ';
                        space = false;
                    }

                    if (length == 0)
                        // start of token
                        start = offset + bufferIndex - 1;
                    else if (length == buffer.Length)
                        buffer = termAtt.ResizeTermBuffer(1 + length);

                    buffer[length++] = Normalize(c); // buffer it, normalized

                    if (length == MAX_WORD_LEN)
                        // buffer overflow!
                        break;
                }
                else if (length > 0)
                {
                    space = true;
                }
            }

            termAtt.SetTermLength(length);
            offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(start + length + excluded));
            return true;
        }

        public override void End()
        {
            // set final offset
            int finalOffset = CorrectOffset(offset);
            offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        public override void Reset(System.IO.TextReader input)
        {
            base.Reset(input);
            bufferIndex = 0;
            offset = 0;
            dataLen = 0;
        }
    }

    public class CharJoinAbbrLowerCaseReplacementAnalyzer : Analyzer
    {
        //this wont currently work as non-letter characters are stripped before synonymfilter is executed.
        //private static List<List<string>> synonyms = new List<List<string>>
        //{
        //	new List<string>
        //	{
        //		"and",
        //		"&",
        //		"+"
        //	}
        //};

        //private static StringSynonymEngine engine = new StringSynonymEngine(synonyms);

        public override TokenStream TokenStream(System.String fieldName, System.IO.TextReader reader)
        {
            TokenStream result = (TokenStream)new ReplacerTokenizer(reader);
            result = new CharJoinAbbreviationsLowerCaseFilter(result);
            //result = new SynonymFilter(result, engine);

            return result;
        }

        public override TokenStream ReusableTokenStream(System.String fieldName, System.IO.TextReader reader)
        {
            SavedStreams streams = (SavedStreams)PreviousTokenStream;
            if (streams == null)
            {
                streams = new SavedStreams(this);
                streams.source = new CharJoinAbbreviationsLowerCaseTokenizer(reader);
                //streams.result = new SynonymFilter(streams.source, engine);
                streams.result = new CharJoinAbbreviationsLowerCaseFilter(streams.source);
                //streams.result = streams.source; //if we dont have a filter above
                PreviousTokenStream = streams;
            }
            else
                streams.source.Reset(reader);
            return streams.result;
        }

        private class SavedStreams
        {
            public SavedStreams(CharJoinAbbrLowerCaseReplacementAnalyzer enclosingInstance)
            {
                InitBlock(enclosingInstance);
            }
            private void InitBlock(CharJoinAbbrLowerCaseReplacementAnalyzer enclosingInstance)
            {
                this.enclosingInstance = enclosingInstance;
            }
            private CharJoinAbbrLowerCaseReplacementAnalyzer enclosingInstance;
            public CharJoinAbbrLowerCaseReplacementAnalyzer Enclosing_Instance
            {
                get
                {
                    return enclosingInstance;
                }

            }
            internal Tokenizer source;
            internal TokenStream result;
        }
    }

    public class Replacement
    {
        public Replacement() { }

        public Replacement(string key, string value)
        {
            Key = key.ToArray();
            Value = value.ToArray();
        }

        public char[] Key { get; set; }
        public char[] Value { get; set; }
    }

    public class ReplacerTokenizer : Tokenizer
    {
        private static List<KeyValuePair<string, string>> replacements = new List<KeyValuePair<string, string>>
		{
			new KeyValuePair<string, string> ("&", "and"),
			new KeyValuePair<string, string> ("+", "and")
		};
        private static List<Replacement> _replacements = null;
        private static List<Replacement> Replacements
        {
            get
            {
                if (_replacements == null)
                {
                    _replacements = replacements.Select(x => new Replacement(x.Key, x.Value)).ToList();
                }

                return _replacements;
            }
        }

        protected void Init()
        {
            offsetAtt = AddAttribute<IOffsetAttribute>();
            termAtt = AddAttribute<ITermAttribute>();
        }

        public ReplacerTokenizer(System.IO.TextReader input)
            : base(input) { Init(); }

        public ReplacerTokenizer(AttributeSource source, System.IO.TextReader input)
            : base(source, input) { Init(); }

        public ReplacerTokenizer(AttributeFactory factory, System.IO.TextReader input)
            : base(factory, input) { Init(); }

        private int offset = 0, bufferIndex = 0, dataLen = 0;
        private const int MAX_WORD_LEN = 255;
        private const int IO_BUFFER_SIZE = 4096;
        private char[] ioBuffer = new char[IO_BUFFER_SIZE];

        private ITermAttribute termAtt;
        private IOffsetAttribute offsetAtt;

        protected virtual bool IsTokenChar(char c)
        {
            return !System.Char.IsWhiteSpace(c);
        }

        public override bool IncrementToken()
        {
            ClearAttributes();
            int length = 0, chars = 0;
            int start = bufferIndex;
            char[] buffer = termAtt.TermBuffer();
            while (true)
            {
                if (bufferIndex >= dataLen)
                {
                    offset += dataLen;
                    dataLen = input.Read((System.Char[])ioBuffer, 0, ioBuffer.Length);
                    if (dataLen <= 0)
                    {
                        dataLen = 0; // so next offset += dataLen won't decrement offset
                        if (length > 0)
                            break;
                        else
                            return false;
                    }
                    bufferIndex = 0;
                }

                char c = ioBuffer[bufferIndex++];

                if (IsTokenChar(c))
                {
                    if (length == 0)
                        // start of token
                        start = offset + bufferIndex - 1;
                    else if (length == buffer.Length)
                        buffer = termAtt.ResizeTermBuffer(1 + length);

                    buffer[length++] = c; // buffer it, normalized

                    if (length == MAX_WORD_LEN)
                        // buffer overflow!
                        break;
                }
                else if (length > 0)
                    // at non-Letter w/ chars
                    break; // return 'em
            }

            chars = length;

            if (length > 0)
            {
                var term = new char[length];
                Array.Copy(buffer, 0, term, 0, length);

                var r = Replacements.FirstOrDefault(x => x.Key.SequenceEqual(term));
                if (r != null)
                {
                    length = r.Value.Length;
                    termAtt.ResizeTermBuffer(length);
                    termAtt.SetTermBuffer(r.Value, 0, length);
                }
            }

            termAtt.SetTermLength(length);
            offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(start + chars));
            return true;
        }

        public override void End()
        {
            // set final offset
            int finalOffset = CorrectOffset(offset);
            offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        public override void Reset(System.IO.TextReader input)
        {
            base.Reset(input);
            bufferIndex = 0;
            offset = 0;
            dataLen = 0;
        }
    }

    public class CharJoinAbbreviationsLowerCaseTokenizer : Tokenizer
    {
        public CharJoinAbbreviationsLowerCaseTokenizer(System.IO.TextReader input)
            : base(input)
        {
            offsetAtt = AddAttribute<IOffsetAttribute>();
            termAtt = AddAttribute<ITermAttribute>();
        }

        public CharJoinAbbreviationsLowerCaseTokenizer(AttributeSource source, System.IO.TextReader input)
            : base(source, input)
        {
            offsetAtt = AddAttribute<IOffsetAttribute>();
            termAtt = AddAttribute<ITermAttribute>();
        }

        public CharJoinAbbreviationsLowerCaseTokenizer(AttributeFactory factory, System.IO.TextReader input)
            : base(factory, input)
        {
            offsetAtt = AddAttribute<IOffsetAttribute>();
            termAtt = AddAttribute<ITermAttribute>();
        }

        private int offset = 0, bufferIndex = 0, dataLen = 0;
        private const int MAX_WORD_LEN = 255;
        private const int IO_BUFFER_SIZE = 4096;
        private char[] ioBuffer = new char[IO_BUFFER_SIZE];

        private ITermAttribute termAtt;
        private IOffsetAttribute offsetAtt;

        protected virtual bool IsTokenChar(char c)
        {
            return System.Char.IsLetterOrDigit(c) || IsTokenCharButExcluded(c);
        }

        protected virtual bool IsTokenCharButExcluded(char c)
        {
            return c == '.' || c == '\'';
        }

        protected virtual char Normalize(char c)
        {
            return System.Char.ToLower(c);
        }

        public override bool IncrementToken()
        {
            ClearAttributes();
            int length = 0, excluded = 0;
            int start = bufferIndex;
            char[] buffer = termAtt.TermBuffer();
            while (true)
            {
                if (bufferIndex >= dataLen)
                {
                    offset += dataLen;
                    dataLen = input.Read((System.Char[])ioBuffer, 0, ioBuffer.Length);
                    if (dataLen <= 0)
                    {
                        dataLen = 0; // so next offset += dataLen won't decrement offset
                        if (length > 0)
                            break;
                        else
                            return false;
                    }
                    bufferIndex = 0;
                }

                char c = ioBuffer[bufferIndex++];

                if (IsTokenChar(c))
                {
                    // if it's a token char

                    if (IsTokenCharButExcluded(c))
                    {
                        excluded++;
                        continue;
                    }

                    if (length == 0)
                        // start of token
                        start = offset + bufferIndex - 1;
                    else if (length == buffer.Length)
                        buffer = termAtt.ResizeTermBuffer(1 + length);

                    buffer[length++] = Normalize(c); // buffer it, normalized

                    if (length == MAX_WORD_LEN)
                        // buffer overflow!
                        break;
                }
                else if (length > 0)
                    // at non-Letter w/ chars
                    break; // return 'em
            }

            termAtt.SetTermLength(length);
            offsetAtt.SetOffset(CorrectOffset(start), CorrectOffset(start + length + excluded));
            return true;
        }

        public override void End()
        {
            // set final offset
            int finalOffset = CorrectOffset(offset);
            offsetAtt.SetOffset(finalOffset, finalOffset);
        }

        public override void Reset(System.IO.TextReader input)
        {
            base.Reset(input);
            bufferIndex = 0;
            offset = 0;
            dataLen = 0;
        }
    }

    public class BufferWithOffset
    {
        public BufferWithOffset(char[] buffer, int? offset) { Buffer = buffer; Offset = offset; }

        public char[] Buffer { get; set; }
        public int? Offset { get; set; }
    }

    public class CharJoinAbbreviationsLowerCaseFilter : TokenFilter
    {
        private Queue<BufferWithOffset> sTermQueue = new Queue<BufferWithOffset>();

        public CharJoinAbbreviationsLowerCaseFilter(TokenStream input)
            : base(input)
        {
            termAtt = AddAttribute<ITermAttribute>();
            offsetAtt = AddAttribute<IOffsetAttribute>();
            posIncAtt = AddAttribute<IPositionIncrementAttribute>();
        }

        private ITermAttribute termAtt;
        private IOffsetAttribute offsetAtt;
        private IPositionIncrementAttribute posIncAtt;

        protected virtual bool IsTokenChar(char c)
        {
            return System.Char.IsLetterOrDigit(c) || IsTokenCharButExcluded(c);
        }

        protected virtual bool IsTokenCharButExcluded(char c)
        {
            return c == '.' || c == '\'';
        }

        protected virtual char Normalize(char c)
        {
            return System.Char.ToLower(c);
        }

        public override bool IncrementToken()
        {
            if (sTermQueue.Count > 0)
            {
                var t = sTermQueue.Dequeue();
                termAtt.ResizeTermBuffer(t.Buffer.Length);
                termAtt.SetTermLength(t.Buffer.Length);
                termAtt.SetTermBuffer(t.Buffer, 0, t.Buffer.Length);
                if (t.Offset.HasValue) posIncAtt.PositionIncrement = t.Offset.Value;

                return true;
            }

            while (input.IncrementToken())
            {
                int length = termAtt.TermLength();
                char[] buffer = termAtt.TermBuffer();
                int n = 0;
                for (var i = 0; i < length; i++)
                {
                    char c = buffer[i];

                    if (IsTokenChar(c))
                    {
                        if (IsTokenCharButExcluded(c))
                            continue;

                        buffer[n++] = Normalize(c);
                    }
                    else if (n > 0)
                    {
                        var tbuffer = new char[n];
                        Array.Copy(buffer, 0, tbuffer, 0, n);

                        sTermQueue.Enqueue(new BufferWithOffset(tbuffer, null));

                        if (tbuffer.Last() == 's')
                        {
                            var tbuffer2 = new char[n - 1];
                            Array.Copy(buffer, 0, tbuffer2, 0, n - 1);
                            sTermQueue.Enqueue(new BufferWithOffset(tbuffer2, 0));
                        }

                        n = 0;
                    }
                }

                if (n > 0)
                {
                    var tbuffer = new char[n];
                    Array.Copy(buffer, 0, tbuffer, 0, n);

                    sTermQueue.Enqueue(new BufferWithOffset(tbuffer, null));

                    if (tbuffer.Last() == 's')
                    {
                        var tbuffer2 = new char[n - 1];
                        Array.Copy(buffer, 0, tbuffer2, 0, n - 1);
                        sTermQueue.Enqueue(new BufferWithOffset(tbuffer2, 0));
                    }
                }

                if (sTermQueue.Count > 0)
                {
                    var t = sTermQueue.Dequeue();
                    termAtt.ResizeTermBuffer(t.Buffer.Length);
                    termAtt.SetTermLength(t.Buffer.Length);
                    termAtt.SetTermBuffer(t.Buffer, 0, t.Buffer.Length);
                    if (t.Offset.HasValue) posIncAtt.PositionIncrement = t.Offset.Value;

                    return true;
                }
            }

            return false;
        }
    }

    public class SynonymFilter : TokenFilter
    {
        private Queue<char[]> sTermQueue = new Queue<char[]>();
        public ISynonymEngine SynonymEngine { get; private set; }

        public SynonymFilter(TokenStream input, ISynonymEngine synonymEngine)
            : base(input)
        {
            SynonymEngine = synonymEngine;

            termAtt = AddAttribute<ITermAttribute>();
            posIncAtt = AddAttribute<IPositionIncrementAttribute>();
            typeAtt = AddAttribute<ITypeAttribute>();
        }

        private ITermAttribute termAtt;
        private IPositionIncrementAttribute posIncAtt;
        private ITypeAttribute typeAtt;

        public override bool IncrementToken()
        {
            if (sTermQueue.Count > 0)
            {
                var sbuffer = sTermQueue.Dequeue();
                typeAtt.Type = "<SYNONYM>";
                posIncAtt.PositionIncrement = 0;
                termAtt.ResizeTermBuffer(sbuffer.Length);
                termAtt.SetTermBuffer(sbuffer, 0, sbuffer.Length);
                termAtt.SetTermLength(sbuffer.Length);
                return true;
            }

            if (!input.IncrementToken())
                return false;

            var buffer = termAtt.TermBuffer();
            var len = termAtt.TermLength();
            var synonyms = SynonymEngine.GetSynonyms(buffer, len);

            if (synonyms != null)
            {
                foreach (var syn in synonyms)
                {
                    if (!buffer.SequenceEqual(syn))
                        sTermQueue.Enqueue(syn);
                }
            }

            return true;
        }
    }

    public interface ISynonymEngine
    {
        IEnumerable<char[]> GetSynonyms(char[] buffer, int length);
    }

    public class StringSynonymEngine : ISynonymEngine
    {
        private List<List<char[]>> synonyms;

        public StringSynonymEngine(List<List<string>> synonyms)
        {
            this.synonyms = synonyms.Select(x => x.Select(y => y.ToArray()).ToList()).ToList();
        }

        public IEnumerable<char[]> GetSynonyms(char[] buffer, int length)
        {
            var word = new char[length];
            Array.Copy(buffer, 0, word, 0, length);

            foreach (var group in synonyms)
            {
                for (var i = 0; i < group.Count; i++)
                {
                    var syn = group[i];

                    if (word.SequenceEqual(syn))
                    {
                        var result = new List<char[]>(group);
                        result.RemoveAt(i);

                        return result;
                    }
                }
            }

            return null;
        }
    }
}