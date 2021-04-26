using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Common
{
    public static class MinimumEditDistance<T> where T:IEquatable<T>
    {
        public static Func<T, T, bool> Equal { get; set; } = null;
        public static MedCell FinalCell { get; set; } = new MedCell { Delete = 0, Insert = 0, Substitution = 0, Same = 0 };
        private static int N = 0;
        private static MedCell[] ItorationArray = new MedCell[0];
        private static T[] StandardArray = new T[0];
        private static T[] CompareArray = new T[0];
        private static MedCell Diag = new MedCell { Insert = 0, Delete = 0, Substitution = 0, Same = 0 };
        private static int[,] MedMatrix = new int[0, 0];
        public static bool IsUnique { get; set; } = true;
        public static MedCell RunWithoutBackTrack(IEnumerable<T> standardSequance, IEnumerable<T> compareSequence)
        {
            StandardArray = standardSequance.ToArray();
            N = StandardArray.Length;
            ItorationArray = Enumerable.Range(0, N + 1).Select(x => new MedCell { Delete = x }).ToArray();
            return Itorate(compareSequence);
        }
        public static int RunWithBackTrack(T[] standardArray, T[] compareArray)
        {
            StandardArray = standardArray;
            CompareArray = compareArray;
            MedMatrix = new int[compareArray.Length + 1, standardArray.Length + 1];
            for (int i = 1; i <= standardArray.Length; i++)
                MedMatrix[0, i] = i;
            for (int i = 1; i <= compareArray.Length; i++)
                MedMatrix[i, 0] = i;
            for(int i = 1; i <= compareArray.Length; i++)
            {
                for(int j = 1; j <= standardArray.Length; j++)
                {
                    int top = MedMatrix[i - 1, j] + 1;
                    int left = MedMatrix[i, j - 1] + 1;
                    int diag = MedMatrix[i - 1, j - 1];
                    if (!standardArray[j - 1].Equals(compareArray[i - 1]))
                        diag++;
                    MedMatrix[i, j] = Math.Min(diag, Math.Min(top, left));
                }
            }
            return MedMatrix[compareArray.Length, standardArray.Length];
        }
        public static IEnumerable<(T,T)> BackTrack(T tDefault)
        {
            int ins = 0;
            int del = 0;
            int sub = 0;
            int same = 0;
            int i = CompareArray.Length;
            int j = StandardArray.Length;
            IsUnique = true;
            while (i >= 0 && j >= 0)
            {
                if (i == 0 && j == 0)
                    break;
                if (i == 0)
                {
                    yield return (StandardArray[j-1], tDefault);
                    j--;
                    del++;
                    continue;
                }
                if (j == 0)
                {
                    yield return (tDefault, CompareArray[i - 1]);
                    i--;
                    ins++;
                    continue;
                }

                int diag = MedMatrix[i - 1, j - 1];
                if (!StandardArray[j - 1].Equals(CompareArray[i - 1]))
                    diag++;
                int top = MedMatrix[i - 1, j] + 1;
                int left = MedMatrix[i, j - 1] + 1;
                int current = MedMatrix[i, j];
                var tmp = new int[] { top, left, diag };
                if (tmp.Where(x => x == current).Count() > 1)
                    IsUnique = false;
                if (current == diag&&(StandardArray[j-1].Equals(CompareArray[i-1])))
                {
                    yield return (StandardArray[j - 1], CompareArray[i - 1]);
                    i--;
                    j--;
                    same++;
                    continue;
                }
                if (current == diag && !StandardArray[j - 1].Equals(CompareArray[i - 1]))
                {
                    yield return (StandardArray[j - 1], CompareArray[i - 1]);
                    i--;
                    j--;
                    sub++;
                    continue;
                }
                if (current == left)
                {
                    yield return (StandardArray[j - 1], tDefault);
                    j--;
                    del++;
                    continue;
                }
                if (current == top)
                {
                    yield return (tDefault, CompareArray[i - 1]);
                    i--;
                    ins++;
                    continue;
                }
                throw new CommonException("Invalid MED.");
            }
            FinalCell = new MedCell { Delete = del, Insert = ins, Substitution = sub, Same = same };            
        }

        private static MedCell Itorate(IEnumerable<T> compareSequence)
        {
            int index = 0;
            foreach (var t in compareSequence)
            {
                Diag = new MedCell { Insert = index};
                ItorationArray[0] = new MedCell { Insert = index + 1 };
                index++;
                for (int i = 1; i <= N; i++)
                {
                    var left = Delete(ItorationArray[i - 1]);
                    var top = Insert(ItorationArray[i]);
                    var diag = Equal(t, StandardArray[i - 1]) ? Same(Diag) : Substitute(Diag);
                    var min = Sequence.ArgMax((x, y) => Value(x) < Value(y), left, top, diag);
                    Diag = ItorationArray[i];
                    ItorationArray[i] = min;
                }
            }
            return ItorationArray[N];
        }
        private static int Value(MedCell cell)
        {
            return cell.Insert + cell.Delete + cell.Substitution;
        }
        private static MedCell Insert(MedCell cell)
        {
            return new MedCell { Delete = cell.Delete, Insert = cell.Insert + 1, Substitution = cell.Substitution, Same = cell.Same };
        }
        private static MedCell Delete(MedCell cell)
        {
            return new MedCell { Delete = cell.Delete + 1, Insert = cell.Insert, Substitution = cell.Substitution, Same = cell.Same };
        }
        private static MedCell Substitute(MedCell cell)
        {
            return new MedCell { Delete = cell.Delete, Insert = cell.Insert, Substitution = cell.Substitution + 1, Same = cell.Same };
        }

        private static MedCell Same(MedCell cell)
        {
            return new MedCell { Delete = cell.Delete, Insert = cell.Insert, Substitution = cell.Substitution, Same = cell.Same + 1 };
        }
    }

    public struct MedCell
    {
        public int Insert { get; set; }
        public int Delete { get; set; }
        public int Substitution { get; set; }
        public int Same { get; set; }
    }
}
