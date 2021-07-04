using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace Tomita
{
    public partial class Form1 : Form
    {
        private RichTextBox textBox;
        private DataGridView dataGrid;
        private Label numberPos;
        private Label numberNeg;
        private Label numberAll;
        private Label answer;

        Dictionary<string, float> tones = new Dictionary<string, float>();

        //string[,] words; //table for:   Word | Noun/Adj | № | Tonality 
        List<List<string>> words = new List<List<string>>();
        int numNoun = 0;
        int numRows = 0;
        int numEx;
        bool isFirstTime;

        Dictionary<string, float> wordsTones = new Dictionary<string, float>();
        public Form1()
        {
            InitializeComponent();
            textBox = this.richTextBox;
            dataGrid= this.dataGridView1;
            getTonalDictionary();
            numberPos = this.labelPos;
            numberNeg = this.labelNeg;
            numberAll = this.labelAll;
            answer = this.labelAns;
        }
        private void getAnswer(int all, int pos, int neg, int neut)
        {
            float d = (float) neut / (float) all;
            float d1 = Math.Abs((float)pos - (float)neg) / all;
            if (pos == neut && pos!=0) answer.Text = "позитивная";
            else if  (neg== neut) answer.Text = "негативная";
            else 
            if ( d>=0.5) answer.Text = "нейтральная";
            else
                if ((d1 <=0.1)) answer.Text = "противоречивая";
            else if (pos>neg) answer.Text = "позитивная";
            else answer.Text = "негативная";
            if (pos == 0 && neg == 0 && neut == 0) answer.Text = "нейтральная";
        }
        private void button_Click(object sender, EventArgs e)
        {
           
           
            numberPos.Text = "0..";
            numberNeg.Text = "0..";
            numberAll.Text = "0..";
            answer.Text = "Определяется...";
            words.Clear();
            wordsTones.Clear();
            dataGrid.Rows.Clear();
            numNoun = 0;
            numRows = 0;
            isFirstTime = true;


            GetText();
            makeCodeTomita(0);
            StartTomita();
            ReadFacts();
            UpdateGrid();

            Console.WriteLine("words.Count    " + words.Count);
            Console.WriteLine("wordsTones.Count    " + wordsTones.Count);
        }


        private void UpdateGrid()
        {
            /*
             * 
             * Word | Noun/Adj | № | Tonality 
             * 
             Алгоритм составления таблицы:
             1. Последовательно брать первый элемент Word, если это Noun
             2. Проверять нет ли его в  wordsTones
                Если есть, то следующий элемент
                Если нет, то получить № элемента 
            3.  Записать значение тональности существительного
            4.  По номеру существительного искать прилагательные и прибавлять их тональность 
            5.  Загрузить словарь в таблицу
             */
            int i = 0; float sum;int j = 0; int counter = 0;
            StringBuilder adjStr= new StringBuilder();
            

            int numNEG = 0;
            int numPOS = 0;
            int numNEUT = 0;

            while (i<words.Count())
            {
             
                if (words[i][1] == "Noun")
                { 
                   if (!isInside(i))
                   {
                        sum = float.Parse(words[i][3]);
                        adjStr.Remove(0, adjStr.Length);

                        j = 0; counter = 0;
                        while (j < words.Count())
                        {
                            if (words[j][1] == "Adj" && words[j][2] == words[i][2])
                            {
                                adjStr.Append(words[j][0] + "(");
                                if (float.Parse(words[j][3])>0.5)
                                {
                                    adjStr.Append("положит");
                                }
                                else 
                                    if (float.Parse(words[j][3]) <-0.3)
                                {
                                    adjStr.Append("негатив");
                                }
                                else
                                {
                                    adjStr.Append("нейтрал");
                                }

                                adjStr.Append(");   ");

                                sum += float.Parse(words[j][3]);
                                counter++;
                            
                            }
                            j++;
                        }
                        wordsTones.Add(words[i][0], sum/counter);
                         if (sum / counter > 0.5)
                            {
                                dataGrid.Rows.Add(words[i][0], "положительная", adjStr.ToString());
                             
                                numPOS++;
                            }
                         else if (sum / counter < -0.3)
                            {
                                dataGrid.Rows.Add(words[i][0], "негативная" , adjStr.ToString());
                               
                            numNEG++;
                            }
                          else
                            {
                                dataGrid.Rows.Add(words[i][0], "нейтральная", adjStr.ToString());
                              
                            numNEUT++;
                            }
                    }
                }
                i++;
            }

            /*foreach (var w in wordsTones)
            {
                if (w.Value > 0.5)
                {
                    dataGrid.Rows.Add(w.Key, "положительная");
                    numPOS++;
                }
                else if (w.Value < -0.3)
                {
                    dataGrid.Rows.Add(w.Key, "негативная");
                    numNEG++;
                }
                else
                {
                    dataGrid.Rows.Add(w.Key, "нейтральная");
                    numNEUT++;
                }
            }*/

            if (words.Count() == 0) MessageBox.Show("Атрибутов не найдено");

            numberPos.Text = numPOS.ToString();
            numberNeg.Text = numNEG.ToString();
            numberAll.Text = (numPOS + numNEG + numNEUT).ToString();
            getAnswer(numPOS + numNEG + numNEUT, numPOS, numNEG, numNEUT);


        }

        private void deleteOutput()
        {

            if (File.Exists(@"C:/tomita/output.txt"))
            {
                File.Delete(@"C:/tomita/output.txt");
            }
        }
        private void makeCodeTomita(int mode)
        {
            /*
            * This void added code to first.cxx to prevent errors like two adj loooks like noun + adj (старший спортивный, мой рабочий)             
            */
            string code;
            if (mode == 0 || mode == 1)
            {
                if (File.Exists(@"C:/tomita/first.cxx"))
                {
                    File.Delete(@"C:/tomita/first.cxx");
                }

             
                    if (mode == 1)
                    {
                        code = @"#encoding ""utf-8""    // сообщаем парсеру о том, в какой кодировке написана грамматика
#GRAMMAR_ROOT S      // указываем корневой нетерминал грамматики

AdjCoord->Adj; //одно прилагатльное 
                AdjCoord->AdjCoord <gnc-agr[1], rt> ',' AdjCoord <gnc-agr[1]>; // прилагательные - однородные члены могут быть записаны через запятую
                AdjCoord->AdjCoord <gnc-agr[1], rt> 'и' AdjCoord <gnc-agr[1]>; // или через сочинительный союз 'и'


                S->Adj interp(FactAdj.A::norm = ""m,sg""); ";


                    }
                    else
                    {
                        code = @"#encoding ""utf-8""    // сообщаем парсеру о том, в какой кодировке написана грамматика
#GRAMMAR_ROOT S      // указываем корневой нетерминал грамматики

AdjCoord->Adj; //одно прилагатльное 
                AdjCoord->AdjCoord <gnc-agr[1], rt> ',' AdjCoord <gnc-agr[1]>; // прилагательные - однородные члены могут быть записаны через запятую
                AdjCoord->AdjCoord <gnc-agr[1], rt> 'и' AdjCoord <gnc-agr[1]>; // или через сочинительный союз 'и'

               S->Noun<gnc-agr[1]> interp(Fact.Noun) AdjCoord<gnc-agr[1]>+ interp(Fact.Adj::norm = ""m,sg"");
S->AdjCoord <gnc-agr[1]> +interp(Fact.Adj::norm = ""m,sg"") Noun <gnc-agr[1]> interp(Fact.Noun); ";
                    }
                //using (StreamWriter sw = new StreamWriter(@"C:/tomita/first.cxx", false, System.Text.Encoding.UTF8))
                //{
                //    sw.WriteLine(code);                 
                //}


                int NumberOfRetries = 3;
                int DelayOnRetry = 1000;

                for (int h = 1; h <= NumberOfRetries; h++)
                {
                    try
                    {

                        using (StreamWriter sw = new StreamWriter(@"C:/tomita/first.cxx", false, System.Text.Encoding.UTF8))
                        {
                            sw.WriteLine(code);
                            sw.Close();
                        }

                        break;
                    }
                    catch (IOException e) when (h <= NumberOfRetries)
                    {
                        Thread.Sleep(DelayOnRetry);
                    }
                }


            }
           
            deleteOutput();
        }

        private bool isInside(int i)
        {
            return wordsTones.ContainsKey(words[i][0]);

        }

        private void GetText()
        {
            if (File.Exists(@"C:/tomita/test.txt"))
            {
                File.Delete(@"C:/tomita/test.txt");
            }
            using (FileStream fstream = new FileStream("C:/tomita/test.txt", FileMode.OpenOrCreate))
            {
                byte[] array = System.Text.UTF8Encoding.UTF8.GetBytes(textBox.Text);

                if (array.Length < 1000000000)
                {
                    fstream.Write(array, 0, array.Length);
                    Console.WriteLine("Текст записан в файл");
                    fstream.Close();
                }
                else
                {
                    MessageBox.Show("Введенный текст слишком велик.");
                }
                fstream.Close();
            }

        }

        private void setAdjtext(string adjstr)
        {
            if (File.Exists(@"C:/tomita/test.txt"))
            {
                File.Delete(@"C:/tomita/test.txt");
            }
            int NumberOfRetries = 3;
            int DelayOnRetry = 1000;

            for (int h = 1; h <= NumberOfRetries; h++)
            {
                try
                {

                    using (FileStream fstream = new FileStream("C:/tomita/test.txt", FileMode.OpenOrCreate))
                    {
                        byte[] array = System.Text.UTF8Encoding.UTF8.GetBytes(adjstr);
                        fstream.Write(array, 0, array.Length);
                        //Console.WriteLine("Текст записан в файл");
                        fstream.Close();
                    }

                    break;
                }
                catch (IOException e) when (h <= NumberOfRetries)
                {
                    Thread.Sleep(DelayOnRetry);
                }
            }
        }

        private void StartTomita()
        {
            Process cmd = new Process();
            cmd.StartInfo.FileName = "cmd.exe";
            cmd.StartInfo.RedirectStandardInput = true;
            cmd.StartInfo.RedirectStandardOutput = true;
            cmd.StartInfo.CreateNoWindow = true;
            cmd.StartInfo.UseShellExecute = false;
            cmd.Start();
            cmd.StandardInput.WriteLine(@"cd C:/tomita");
            cmd.StandardInput.WriteLine(@"tomitaparser.exe config.proto");
            cmd.StandardInput.Flush();
            cmd.StandardInput.Close();
            cmd.Close();
        }

        private void getTonalDictionary()
        {
            string temp;
            StringBuilder word = new StringBuilder();
            StringBuilder toneStr = new StringBuilder();
            temp = File.ReadAllText(@"C:/tomita/tonalDic.txt", Encoding.UTF8);
            int i = 0;
            while (i < temp.Length-10)
            {
                
                if (temp[i]=='\n')
                {
                    word.Remove(0, word.Length);
                    i++;
                    while (temp[i] != ';')
                    {
                        word.Append(temp[i]);
                        i++;
                    }
                    i++;
                    while (temp[i] != ';') i++;
                    toneStr.Remove(0, toneStr.Length);
                    i++;
                    while (temp[i] != ';')
                    {
                        toneStr.Append(temp[i]);
                        i++;
                    }
                    tones.Add(word.ToString(), float.Parse(toneStr.ToString()));
                }
                i++;
            }

        }

        private float getWorldTone(string word)
        {

            string str = word.Replace("е", "ё");
            if (tones.ContainsKey(word))
                return tones.Where(p => p.Key == word).Select(tones => tones.Value).First();
            else
            {
                if (word.Contains("e"))
                {
                   if (tones.ContainsKey(str)) 
                        return tones.Where(p => p.Key == str).Select(tones => tones.Value).First();
                }
              
                return 0;
            }

        }

        private void addWord(string word, float tone, bool isNoun) //adding the word to array words
        {
            /*
             * Если существительное, то нужно проверить было ли уже такое и если было, то то присвоить его номер
            Слова добавляются друг за другом. Цепочки отличаются номером. Номер определяетя по найденному существительному
            Прилагательные дополнительно разбиваются

             Word | Noun/Adj | № | Tonality 
             */
            int i = 0; bool isEnd = false;
            // string temp=""; 
            StringBuilder temp= new StringBuilder();
            while (isEnd!=true)
            { 
                if (isNoun)             
                {
                    numEx = -1;
                    for (int j =0; j < words.Count(); j++)
                    {
                        if (String.Equals(words[j][0], word))
                        {
                            numEx = int.Parse(words[j][2]);

                        }
                    }
                    
                    words.Add(new List<string>());

                    words[numRows].Add(word);
                    words[numRows].Add("Noun");
                   if (numEx ==-1)  words[numRows].Add(numNoun.ToString());
                   else words[numRows].Add(numEx.ToString());
                    words[numRows].Add(tone.ToString());
                    numRows++;
                    isEnd = true;
                }
                if (!isNoun)//if the word is Adj    
                {
                    /*    1. Нужно разбить строку с прилагательным
                     *    
                     *    1.1. Поменять текст в test.txt
                     *    1.2. Запусть парсер 
                     *    1.3. Получить факты из парсера и занести каждое в words
                     *    
                          2. Добавить в words  все полученные прилагательные */

                    if (word.Contains(" "))
                    {
                        setAdjtext(word);
                        if (isFirstTime == true) { isFirstTime = false; makeCodeTomita(1); }
                        else { makeCodeTomita(2); } 
                        StartTomita();
                        {
                            temp.Clear();
                            int NumberOfRetries = 3;
                            int DelayOnRetry = 1000;
                            for (int h = 1; h <= NumberOfRetries; h++)
                            {
                                try
                                {
                                    using (StreamReader sr = new StreamReader(@"C:/tomita/output.txt", Encoding.UTF8))
                                    {

                                        temp.Append(sr.ReadToEnd());
                                        sr.Close();
                                    }
                                    break; 
                                }
                                catch (IOException e) when (h <= NumberOfRetries)
                                {
                                    Thread.Sleep(DelayOnRetry);
                                }
                            }

                               

                        int num = 0;
                        StringBuilder w = new StringBuilder();
                        i = 0;
                        while (num < temp.ToString().Length - 5)
                        {
                            if (temp.ToString()[num] == 'A' && temp.ToString()[num + 1] == ' ' && temp.ToString()[num + 2] == '=')
                            {
                                num += 4;
                                int j = num; i = 0;
                                w.Remove(0, w.Length);
                                while (temp.ToString()[j] != '\n')
                                {
                                    w.Append(temp.ToString()[j]);
                                    j++; i++;
                                }
                                words.Add(new List<string>());

                                words[numRows].Add(w.ToString());
                                words[numRows].Add("Adj");
                                if (numEx==-1) words[numRows].Add(numNoun.ToString());
                                else words[numRows].Add(numEx.ToString());
                                words[numRows].Add(getWorldTone(w.ToString()).ToString());
                                numRows++;
                                isEnd = true;
                            }
                            num++;
                        }
                        }
                    }
                    else
                    {
                        words.Add(new List<string>());
                        words[numRows].Add(word.ToString());
                        words[numRows].Add("Adj");
                        if (numEx == -1) words[numRows].Add(numNoun.ToString());
                        else words[numRows].Add(numEx.ToString());
                        words[numRows].Add(getWorldTone(word.ToString()).ToString());
                        numRows++;
                        isEnd = true;
                    }
                }

            }
        }

        private void ReadFacts()
        {
                  string text="";
                 int NumberOfRetries = 3;
                 int DelayOnRetry = 1000;

                for (int h=1; h <= NumberOfRetries; h++) {
                try {

                using (StreamReader sr = new StreamReader(@"C:/tomita/output.txt", Encoding.UTF8))
                {
                    text = sr.ReadToEnd();
                }

                break; 
            }
              catch (IOException e) when (h <= NumberOfRetries)
            {
                Thread.Sleep(DelayOnRetry);
            }
        }
                int num = 0;
                StringBuilder word = new StringBuilder();
                int i = 0;
                while (num < text.Length - 5)
                {
                    if (text[num] == 'N' && text[num + 1] == 'o' && text[num + 2] == 'u' && text[num + 3] == 'n' && text[num + 4] == ' ' && text[num + 5] == '=')
                    {
                        num += 7;
                        int j = num; i = 0;
                        word.Remove(0, word.Length);
                        while (text[j] != '\n')
                        {
                            word.Append(text[j]);
                            j++; i++;
                        }
                        num = j + 2;
                        //функция которая находит в словаре слово  и возвращает тональность getWorldTone
                        //вызов функции addWord(word, ton (float), true)
                        addWord(word.ToString(), getWorldTone(word.ToString()), true);
                        while (!(text[num] == 'A' && text[num + 1] == 'd' && text[num + 2] == 'j' && text[num + 3] == ' ' && text[num + 4] == '='))
                            num++;

                        num += 6;
                        i = 0; word.Remove(0, word.Length);
                        while (text[num] != '\n')
                        {
                            word.Append(text[num]);
                            num++; i++;
                        }
                        addWord(word.ToString(), getWorldTone(word.ToString()), false);
                        numNoun++;
                    }

                    num++;
                }
                num++;
        }
    }
}
