#region Prelude
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Game.GUI.TextPanel;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;

using WSV.IterableInt;

namespace SpaceEngineers.UWBlockPrograms.Test
{
  public sealed class Program : MyGridProgram
  {
    #endregion

    IMyGridTerminalSystem gts;
    IMyTextPanel lcd;

    int Size = 100;
    int BoxSize = 4; 
    int ticks = 0;

    Random generator;
    StringBuilder buffer;

    Vector2 position;
    Vector2 direction;

    Program()
    {
      gts = GridTerminalSystem;
      lcd = gts.GetBlockWithName("LCD") as IMyTextPanel;

      generator = new Random();

      int x = generator.Next() % (Size-BoxSize);
      int y = generator.Next() % (Size-BoxSize);

      int dX = generator.Next() % 2;
      int dY = generator.Next() % 2;

      position = new Vector2((float)x, (float)y);
      direction = new Vector2(dX == 0 ? -1f : 1f, dY % 2 == 0 ? -1f : 1f);
      buffer = new StringBuilder(Size * (Size + 1));
      buffer.Append((char)0, Size * (Size + 1));

      Inicialize();
      Update();
    
      Runtime.UpdateFrequency = UpdateFrequency.Update1;

      Echo("X " + x.ToString() + " Y " + y.ToString());

      Echo("Hours " + DateTime.Now.Hour.ToString());

      Echo(lcd.SurfaceSize.ToString());
    }

    static char rgb(byte r, byte g, byte b) {
      return (char)(0xe100 + (r << 6) + (g << 3) + b);
    }

    void Inicialize() {
      lcd.Font = "Monospace";
      lcd.FontSize = 512f / (37f * 0.7783784f * (float)Size);
      lcd.TextPadding = 0;
      lcd.Alignment = TextAlignment.CENTER;
      lcd.ContentType = ContentType.TEXT_AND_IMAGE;
    }

    void Render() {
      ticks++;

      int verySize = Size + 1;

      for(int i = 0; i < Size * verySize; i++) {
        int x = i % verySize;
        int y = (i - x) / verySize;

        if(x <= Size - 1) {
          if(x >= position.X && x < position.X + BoxSize && y >= position.Y && y < position.Y + BoxSize) {
            buffer[i] = rgb(4, 0, 0);
          }else {
            buffer[i] = rgb(0, 1, 0);
          }
        }else {
          buffer[i] = '\n';
        }

      } 

      lcd.WriteText(buffer);
    }

    public void Update() {
      ticks++;

      Vector2 newPosition = position + direction;

      if(newPosition.X < 0 || newPosition.X > Size - 1 - BoxSize)
        direction.X = -direction.X;

      if(newPosition.Y < 0 || newPosition.Y > Size - 1 - BoxSize)
        direction.Y = -direction.Y;

      position += direction;
    }

    public void Main()
    {
      if(ticks % 2 == 1) {
        Update();
      }

      if(ticks % 2 == 0) {
        Render();
      }
    }

    #region PreludeFooter
  }
}
#endregion