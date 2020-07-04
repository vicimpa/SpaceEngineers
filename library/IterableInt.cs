namespace WSV.IterableInt
{
  public class IterableInt
  {
    int now = 0, max = 0;

    public IterableInt(int inputMax) {
      max = inputMax;
    }

    public int GetNext() {
      now++;

      if(now > max)
        now = 0;

      return now;
    }
  }
}