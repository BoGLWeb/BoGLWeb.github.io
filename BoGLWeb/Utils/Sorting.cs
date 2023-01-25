namespace BoGLWeb.Utils {
    public class Sorting {
        private static int getMaxVal(IReadOnlyList<int> array, int size) {
            int maxVal = array[0];

            for (int i = 1; i < size; i++) {
                if (array[i] > maxVal) {
                    maxVal = array[i];
                }
            }

            return maxVal;
        }

        public static List<int> countingSort(List<int> array) {
            int size = array.Count;
            int maxElement = getMaxVal(array, size);
            int[] occurrences = new int[maxElement + 1];

            for (int i = 0; i < maxElement + 1; i++) {
                occurrences[i] = 0;
            }

            for (int i = 0; i < size; i++) {
                occurrences[array[i]]++;
            }

            for (int i = 0, j = 0; i <= maxElement; i++) {
                while (occurrences[i] > 0) {
                    array[j] = i;
                    j++;
                    occurrences [i]--;
                }
            }

            return array;
        }
    }
}