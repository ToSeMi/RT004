using Util;
namespace check1
{
    static class DrawSomething
    {

        static void SetPoints(FloatImage fi, int x, int y, float[] color)
        {
            fi.PutPixel(x, y, color);
            fi.PutPixel(-x, y, color);
            fi.PutPixel(x, -y, color);
            fi.PutPixel(-x, -y, color);
        }

        //Algorithm rewritten from https://cgg.mff.cuni.cz/~pepca/lectures/pdf/icg-13-curves.pdf, slightly modified in the second part
        static void MidpointDraw(FloatImage fi, int a, int b, float[] color)
        {
            int x = 0, y = b;
            double aa = Math.Sqrt(a), aa2 = 2 * aa, bb = (int)Math.Sqrt(b), bb2 = 2 * bb;
            double D = (bb - aa * b + aa) / 4;
            double dx = bb2, dy = aa * (2 * b - 1);
            SetPoints(fi, 0, b, color);
            while (dx < dy)
            {
                if (D >= 0)
                {
                    D = D - dy + aa;
                    dy = dy - aa2;
                    y--;
                }
                D += dx + bb;
                dx += bb2;
                x++;
                SetPoints(fi, x, y, color);
            }
            D = bb * (Math.Sqrt(x) + x) + bb / 4 + aa * (Math.Sqrt(y - 1) - bb);
            while (0 < y)
            {
                if (D < 0)
                {
                    D += dx;
                    dx = dy - aa2;
                    x++;
                }
                D = D - dy + aa2;
                dy -= aa2;
                y--;
                SetPoints(fi, x, y, color);
            }
        }

        static void FloodQueue(FloatImage fi, int x, int y, float[] newColor, float[] oldColor)
        {
            Queue<(int, int)> queue = new();
            (int, int) currentState = (x, y);
            HashSet<(int, int)> visited = new();
            queue.Enqueue(currentState);
            while (queue.Count > 0)
            {
                currentState = queue.Dequeue();
                int[] plusminus = new int[] { -1, 0, 1 };
                foreach (int i in plusminus)
                {
                    foreach (int j in plusminus)
                    {
                        if (currentState.Item1 + i < fi.Width && currentState.Item2 + j < fi.Height
                        && currentState.Item1 + i >= 0 && currentState.Item2 + j >= 0)
                        {
                            float[] newArr = new float[3];
                            fi.GetPixel(currentState.Item1 + i, currentState.Item2 + j, newArr);
                            if (
                                i != j

                            //if i am really rewriting an old color
                            && Math.Abs(newArr[0] - oldColor[0]) < 0.0001f
                            && Math.Abs(newArr[1] - oldColor[1]) < 0.0001f
                            && Math.Abs(newArr[2] - oldColor[2]) < 0.0001f
                            && !visited.Contains((currentState.Item1 + i, currentState.Item2 + j)))
                            {
                                queue.Enqueue((currentState.Item1 + i, currentState.Item2 + j));
                                visited.Add((currentState.Item1 + i, currentState.Item2 + j));
                            }
                        }
                    }
                }
                fi.PutPixel(currentState.Item1, currentState.Item2, newColor);
                visited.Add(currentState);
            }
        }

    }
}
