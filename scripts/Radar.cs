#region Prelude
using System;
using System.Linq;
using System.Text;
using System.Collections;
using System.Collections.Generic;

using VRageMath;
using VRage.Game;
using VRage.Collections;
using Sandbox.ModAPI.Ingame;
using VRage.Game.Components;
using VRage.Game.ModAPI.Ingame;
using Sandbox.ModAPI.Interfaces;
using Sandbox.Game.EntityComponents;
using SpaceEngineers.Game.ModAPI.Ingame;
using VRage.Game.ObjectBuilders.Definitions;

namespace SpaceEngineers.UWBlockPrograms.Radar
{
  public sealed class Program : MyGridProgram
  {
    #endregion

    string OUTPUT = "Cockpit";
    string RADAR = "Radar";
    string TORPEDO = "Torpedo";
    static bool CENTER_SHOT = true;
    static float LOCK_POINT_DEPTH = 5;
    static int LAUNCH_DELAY = 300;
    static float INTERCEPT_COURSE = 1.0f;
    static float MAX_VELOCITY = 100;
    static int WH_ARM_DIST = 100;
    static float TORPEDO_REFLECT_K = 2f;
    static float TORPEDO_GYRO_MULT = 2.5f;
    static float ACCEL_DET = 1.0f;
    static int WARHEAD_TIMER = 300;

    static int WOLF_PACK_WELDING_TIME = 1200;
    static int WOLF_PACK_INTERVAL = 180;
    static int WOLF_PACK_COUNT = 4;

    static IMyGridTerminalSystem gts;
    static int Tick = 0;
    IMyTextSurface iface;
    Radar radar;
    List<Torpedo> Torpedos;

    bool WolfPack = false;
    int WolfPackStart = 0;
    int WolfPackIndex = 0;
    List<int> WolfPackDelays;

    Program()
    {
      gts = GridTerminalSystem;

      var obj = gts.GetBlockWithName(OUTPUT);

      if (obj == null)
      {
        Echo("No find output!");
        return;
      }

      var name = obj.GetType().Name;

      switch (name)
      {
        case "MyCockpit": iface = (obj as IMyCockpit).GetSurface(0); break;
        case "MyTextPanel": iface = obj as IMyTextSurface; break;
      }

      iface.WriteText("Hello world!");

      Echo("Output: " + iface.ToString());

      radar = new Radar(RADAR);
      Torpedos = new List<Torpedo>();
      InitializeTorpedos();
      WolfPackDelays = new List<int>();
    }

    void InitializeTorpedos()
    {
      Echo("Initializing torpedos: \n");
      int c = 0;
      for (int x = 1; x <= 8; x++)
      {
        string status = "";
        if (Torpedos.FindAll((b) => ((b.status == 1) && (b.Name == TORPEDO + x))).Count == 0)
          if (Torpedo.CheckBlocks(TORPEDO + x, out status))
          {
            Torpedos.Add(new Torpedo(TORPEDO + x));
            c++;
            Echo(status);
          }
      }
      Echo("\n" + c + " new torpedos initialized");
      Echo("\n" + Torpedos.FindAll((b) => ((b.status == 1))).Count + " torpedos ready for launch");
      Echo("\n" + Torpedos.FindAll((b) => ((b.status == 2))).Count + " torpedos on the way");
      Echo("\n" + Torpedos.Count + "torpedos in list");
    }

    void ClearAllTorpedos()
    {
      Torpedos.Clear();
    }

    void CleanGarbage()
    {
      List<int> killList = new List<int>();
      Echo("Cleaning: ");

      foreach (Torpedo t in Torpedos)
      {
        if (!t.CheckIntegrity())
        {
          killList.Add(Torpedos.IndexOf(t));
        }
      }
      Torpedos.RemoveIndices(killList);
      Echo("" + killList.Count + " torpedos trashed\n");
    }

    void Main(string arg, UpdateType uType)
    {
      if (uType == UpdateType.Update1)
      {
        Tick++;
        radar.Update();

        iface.WriteText("LOCKED: " + radar.Locked, false);
        iface.WriteText("\nTarget: " + radar.CurrentTarget.Name + ", tick: " + radar.LastLockTick, true);
        iface.WriteText("\nDistance: " + Math.Round(radar.TargetDistance), true);
        iface.WriteText("\nVelocity: " + Math.Round(radar.CurrentTarget.Velocity.Length()), true);

        foreach (Torpedo t in Torpedos)
        {
          if (t.status == 2)
          {
            t.Update(radar.CurrentTarget, CENTER_SHOT ? radar.CurrentTarget.Position : radar.T);
          }
        }
        if (WolfPack)
        {
          if ((Tick - WolfPackStart + 1) % WOLF_PACK_WELDING_TIME == 0)
          {
            CleanGarbage();
            InitializeTorpedos();
          }
          if ((radar.Locked) && ((Tick - WolfPackStart - 1) % WOLF_PACK_WELDING_TIME == 0))
          {
            foreach (Torpedo t in Torpedos)
            {
              Echo("\nTry Launch: ");
              Echo("\nWPI: " + WolfPackIndex);
              if (t.status == 1)
              {
                WolfPackIndex--;
                t.Launch(WolfPackDelays[WolfPackIndex]);
                break;
              }
            }
            if (WolfPackIndex <= 0)
              WolfPack = false;
          }
        }
      }
      else
      {
        switch (arg)
        {
          case "Lock":
            radar.Lock(true, 10000);
            if (radar.Locked)
              Runtime.UpdateFrequency = UpdateFrequency.Update1;
            else
            {
              iface.WriteText("NO TARGET", false);

              Runtime.UpdateFrequency = UpdateFrequency.None;
            }
            break;
          case "Init":
            CleanGarbage();
            InitializeTorpedos();
            break;
          case "Stop":
            radar.StopLock();
            Runtime.UpdateFrequency = UpdateFrequency.None;
            break;
          case "Launch":
            if (radar.Locked)
              foreach (Torpedo t in Torpedos)
              {
                Echo("\nTry Launch: ");
                if (t.status == 1)
                {
                  Echo("1 go");
                  t.Launch();
                  break;
                }
              }
            else
              Echo("No Target Lock");
            break;
          case "Test":
            Echo("\n Test:" + VerticalDelay(5000.0f, 1700.0f, 300));
            break;
          case "Pack":
            if (radar.Locked)
            {
              WolfPackDelays.Clear();
              WolfPackDelays.Add(LAUNCH_DELAY);
              for (int x = 0; x < WOLF_PACK_COUNT - 1; x++)
              {
                WolfPackDelays.Add(VerticalDelay((float)radar.TargetDistance, (float)(WOLF_PACK_WELDING_TIME - WOLF_PACK_INTERVAL) * 1.666667f, WolfPackDelays[WolfPackDelays.Count - 1]));
              }
              WolfPack = true;
              WolfPackStart = Tick;
              WolfPackIndex = WOLF_PACK_COUNT;
            }
            break;
          default:
            break;
        }

      }
    }

    public class Torpedo
    {
      List<IMyThrust> thrusters;
      List<IMyGyro> gyros;
      List<IMyWarhead> warheads;
      List<IMyBatteryBlock> batteries;
      List<IMyDecoy> decoys;
      IMyShipMergeBlock merge;
      IMyRemoteControl remcon;
      int counter = 0;
      public int status = 0;
      public double MyVelocity = 0;
      private float ReflectK = TORPEDO_REFLECT_K;
      private float GyroMult = TORPEDO_GYRO_MULT;
      public string Name;
      int VerticalDelay = 300;
      // int CowntDown=5;
      // bool StartCountDown =false;

      public Torpedo(string GroupName)
      {
        Name = GroupName;
        List<IMyTerminalBlock> templist = new List<IMyTerminalBlock>();
        templist.Clear();
        gts.GetBlocksOfType<IMyShipMergeBlock>(templist, (b) => b.CustomName.Contains(GroupName));
        merge = templist[0] as IMyShipMergeBlock;
        templist.Clear();
        gts.GetBlocksOfType<IMyRemoteControl>(templist, (b) => b.CustomName.Contains(GroupName));
        remcon = templist[0] as IMyRemoteControl;
        batteries = new List<IMyBatteryBlock>();
        gts.GetBlocksOfType<IMyBatteryBlock>(batteries, (b) => b.CustomName.Contains(GroupName));
        thrusters = new List<IMyThrust>();
        gts.GetBlocksOfType<IMyThrust>(thrusters, (b) => b.CustomName.Contains(GroupName));
        gyros = new List<IMyGyro>();
        gts.GetBlocksOfType<IMyGyro>(gyros, (b) => b.CustomName.Contains(GroupName));
        warheads = new List<IMyWarhead>();
        gts.GetBlocksOfType<IMyWarhead>(warheads, (b) => b.CustomName.Contains(GroupName));
        decoys = new List<IMyDecoy>();
        gts.GetBlocksOfType<IMyDecoy>(decoys, (b) => b.CustomName.Contains(GroupName));
        status = 1;
      }

      static public bool CheckBlocks(string GroupName, out string strBlockCheck)
      {
        strBlockCheck = GroupName;
        List<IMyTerminalBlock> templist = new List<IMyTerminalBlock>();

        //---------- MERGE ------------
        templist.Clear();
        gts.GetBlocksOfType<IMyShipMergeBlock>(templist, (b) => b.CustomName.Contains(GroupName));
        strBlockCheck += "\nMerge Blocks: " + templist.Count;
        if (templist.Count == 0)
          return false;

        templist.Clear();
        gts.GetBlocksOfType<IMyShipMergeBlock>(templist, (b) => (b.CustomName.Contains(GroupName) && (b as IMyShipMergeBlock).IsConnected));
        strBlockCheck += "   connected: " + templist.Count;

        //---------- REM CON ------------
        templist.Clear();
        gts.GetBlocksOfType<IMyRemoteControl>(templist, (b) => b.CustomName.Contains(GroupName));
        strBlockCheck += "\nRemCons: " + templist.Count;
        if (templist.Count == 0)
          return false;

        templist.Clear();
        gts.GetBlocksOfType<IMyRemoteControl>(templist, (b) => (b.CustomName.Contains(GroupName) && b.IsFunctional));
        strBlockCheck += "   functional: " + templist.Count;
        if (templist.Count == 0)
          return false;

        //---------- BATTERY ------------
        templist.Clear();
        gts.GetBlocksOfType<IMyBatteryBlock>(templist, (b) => b.CustomName.Contains(GroupName));
        strBlockCheck += "\nBatteries: " + templist.Count;
        if (templist.Count == 0)
          return false;

        templist.Clear();
        gts.GetBlocksOfType<IMyBatteryBlock>(templist, (b) => (b.CustomName.Contains(GroupName) && b.IsFunctional));
        strBlockCheck += "   functional: " + templist.Count;
        if (templist.Count == 0)
          return false;

        //---------- THRUSTERS ------------
        templist.Clear();
        gts.GetBlocksOfType<IMyThrust>(templist, (b) => b.CustomName.Contains(GroupName));
        strBlockCheck += "\nThrusters: " + templist.Count;
        if (templist.Count == 0)
          return false;

        templist.Clear();
        gts.GetBlocksOfType<IMyThrust>(templist, (b) => (b.CustomName.Contains(GroupName) && b.IsFunctional));
        strBlockCheck += "   functional: " + templist.Count;
        if (templist.Count == 0)
          return false;

        //---------- GYROS ------------
        templist.Clear();
        gts.GetBlocksOfType<IMyGyro>(templist, (b) => b.CustomName.Contains(GroupName));
        strBlockCheck += "\nGyros: " + templist.Count;
        if (templist.Count == 0)
          return false;

        templist.Clear();
        gts.GetBlocksOfType<IMyGyro>(templist, (b) => (b.CustomName.Contains(GroupName) && b.IsFunctional));
        strBlockCheck += "   functional: " + templist.Count;
        if (templist.Count == 0)
          return false;

        //---------- WARHEADS ------------
        templist.Clear();
        gts.GetBlocksOfType<IMyWarhead>(templist, (b) => b.CustomName.Contains(GroupName));
        strBlockCheck += "\nWarheads: " + templist.Count;
        if (templist.Count == 0)
          return false;

        templist.Clear();
        gts.GetBlocksOfType<IMyWarhead>(templist, (b) => (b.CustomName.Contains(GroupName) && b.IsFunctional));
        strBlockCheck += "   functional: " + templist.Count;
        if (templist.Count == 0)
          return false;

        //---------- DECOYS ------------
        templist.Clear();
        gts.GetBlocksOfType<IMyDecoy>(templist, (b) => b.CustomName.Contains(GroupName));
        strBlockCheck += "\nDecoys: " + templist.Count;
        if (templist.Count == 0)
          return false;

        templist.Clear();
        gts.GetBlocksOfType<IMyDecoy>(templist, (b) => (b.CustomName.Contains(GroupName) && b.IsFunctional));
        strBlockCheck += "   functional: " + templist.Count;
        if (templist.Count == 0)
          return false;

        strBlockCheck += "\n-------------------------\n";
        return true;
      }

      public bool CheckIntegrity()
      {
        if (!remcon.IsFunctional)
          return false;
        if (batteries.FindAll((b) => (b.IsFunctional)).Count == 0)
          return false;
        if (thrusters.FindAll((b) => (b.IsFunctional)).Count == 0)
          return false;
        if (gyros.FindAll((b) => (b.IsFunctional)).Count == 0)
          return false;
        return true;
      }

      public void Launch(int VertDelay = 0)
      {
        if (VertDelay != 0)
          VerticalDelay = VertDelay;
        else
          VerticalDelay = LAUNCH_DELAY;
        foreach (IMyBatteryBlock bat in batteries)
        {
          bat.Enabled = true;
          bat.ChargeMode = ChargeMode.Discharge;
        }
        foreach (IMyThrust thr in thrusters)
        {
          thr.Enabled = true;
          thr.ThrustOverridePercentage = 1;
        }
        foreach (IMyGyro gyro in gyros)
        {
          gyro.Enabled = true;
          gyro.GyroOverride = true;
          gyro.Pitch = 0;
          gyro.Yaw = 0;
          gyro.Roll = 0;
        }
        foreach (IMyDecoy dec in decoys)
        {
          dec.Enabled = true;
        }
        merge.Enabled = false;
        if (WARHEAD_TIMER > 30)
          foreach (IMyWarhead warhead in warheads)
          {
            warhead.DetonationTime = WARHEAD_TIMER;
            warhead.StartCountdown();
          }
        status = 2;
      }

      public void Update(MyDetectedEntityInfo target, Vector3D T)
      {
        counter++;

        if (counter > VerticalDelay)
        {
          if (remcon.IsFunctional)
          {
            double currentVelocity = remcon.GetShipVelocities().LinearVelocity.Length();
            Vector3D targetvector = FindInterceptVector(remcon.GetPosition(),
                                                        currentVelocity * INTERCEPT_COURSE,
                                                        T,
                                                        target.Velocity);
            Vector3D trgNorm = Vector3D.Normalize(targetvector);

            if ((target.Position - remcon.GetPosition()).Length() < WH_ARM_DIST)
            {
              if (currentVelocity - MyVelocity < -ACCEL_DET)
                //    StartCountDown = true;
                //if (StartCountDown)
                //    CowntDown--;
                //if (CowntDown<=0)
                foreach (IMyWarhead wh in warheads)
                {
                  wh.Detonate();
                }

              MyVelocity = currentVelocity;
            }
            Vector3D velNorm = Vector3D.Normalize(remcon.GetShipVelocities().LinearVelocity);
            Vector3D CorrectionVector = Math.Max(ReflectK * trgNorm.Dot(velNorm), 1) * trgNorm - velNorm;
            Vector3D Axis = Vector3D.Normalize(CorrectionVector).Cross(remcon.WorldMatrix.Forward);
            if (Axis.LengthSquared() < 0.05)
              Axis += remcon.WorldMatrix.Backward * 0.5;
            Axis *= GyroMult;
            foreach (IMyGyro gyro in gyros)
            {
              gyro.Pitch = (float)Axis.Dot(gyro.WorldMatrix.Right);
              gyro.Yaw = (float)Axis.Dot(gyro.WorldMatrix.Up);
              gyro.Roll = (float)Axis.Dot(gyro.WorldMatrix.Backward);
            }
          }
          else
          {
            foreach (IMyGyro gyro in gyros)
            {
              gyro.Pitch = 0;
              gyro.Yaw = 0;
              gyro.Roll = 0;
            }
          }

        }
      }

      private Vector3D FindInterceptVector(Vector3D shotOrigin, double shotSpeed,
          Vector3D targetOrigin, Vector3D targetVel)
      {
        Vector3D dirToTarget = Vector3D.Normalize(targetOrigin - shotOrigin);
        Vector3D targetVelOrth = Vector3D.Dot(targetVel, dirToTarget) * dirToTarget;
        Vector3D targetVelTang = targetVel - targetVelOrth;
        Vector3D shotVelTang = targetVelTang;
        double shotVelSpeed = shotVelTang.Length();

        if (shotVelSpeed > shotSpeed)
        {
          return Vector3D.Normalize(targetVel) * shotSpeed;
        }
        else
        {
          double shotSpeedOrth = Math.Sqrt(shotSpeed * shotSpeed - shotVelSpeed * shotVelSpeed);
          Vector3D shotVelOrth = dirToTarget * shotSpeedOrth;
          return shotVelOrth + shotVelTang;
        }
      }

      private void SetGyro(Vector3D dir)
      {

      }

    }

    public class Radar
    {
      private List<IMyTerminalBlock> CamArray; //массив камер 
      private int CamIndex; //индекс текущей камеры в массиве 
      public MyDetectedEntityInfo CurrentTarget; // структура инфы о захваченном объекте 
      public Vector3D MyPos; // координаты 1й камеры (они и будут считаться нашим положением) 
      public Vector3D correctedTargetLocation; //расчетные координаты захваченного объекта. (прежние координаты+вектор скорости * прошедшее время с последнего обновления захвата) 
      public double TargetDistance; //расстояние до ведомой цели	 
      public int LastLockTick; // программный тик последнего обновления захвата 
      public int TicksPassed; // сколько тиков прошло с последнего обновления захвата 
      public bool Locked;
      public Vector3D T;//Координаты точки первого захвата
      public Vector3D O;//Координаты точки первого захвата лок


      public Radar(string groupname)
      {
        CamIndex = 0;
        Locked = false;
        CamArray = new List<IMyTerminalBlock>();
        IMyBlockGroup RadarGroup = gts.GetBlockGroupWithName(groupname);
        RadarGroup.GetBlocksOfType<IMyCameraBlock>(CamArray);
        foreach (IMyCameraBlock Cam in CamArray)
          Cam.EnableRaycast = true;
      }

      public void Lock(bool TryLock = false, double InitialRange = 10000)
      {
        int initCamIndex = CamIndex++;
        if (CamIndex >= CamArray.Count)
          CamIndex = 0;
        MyDetectedEntityInfo lastDetectedInfo;
        bool CanScan = true;
        // найдем первую после использованной в последний раз камеру, которая способна кастануть лучик на заданную дистанцию. 
        if (CurrentTarget.EntityId == 0)
          TargetDistance = InitialRange;

        while ((CamArray[CamIndex] as IMyCameraBlock)?.CanScan(TargetDistance) == false)
        {
          CamIndex++;
          if (CamIndex >= CamArray.Count)
            CamIndex = 0;
          if (CamIndex == initCamIndex)
          {
            CanScan = false;
            break;
          }
        }
        //если такая камера в массиве найдена - кастуем ей луч. 
        if (CanScan)
        {
          //в случае, если мы осуществляем первоначальный захват цели, кастуем луч вперед 
          if ((TryLock) && (CurrentTarget.IsEmpty()))
          {
            lastDetectedInfo = (CamArray[CamIndex] as IMyCameraBlock).Raycast(InitialRange, 0, 0);
            if ((!lastDetectedInfo.IsEmpty()) && (lastDetectedInfo.Relationship != MyRelationsBetweenPlayerAndBlock.Owner))
            {
              Locked = true;
              Vector3D deep_point = lastDetectedInfo.HitPosition.Value +
                  Vector3D.Normalize(lastDetectedInfo.HitPosition.Value - CamArray[CamIndex].GetPosition()) * LOCK_POINT_DEPTH;
              O = WorldToGrid(lastDetectedInfo.HitPosition.Value, lastDetectedInfo.Position, lastDetectedInfo.Orientation);
            }
          }
          else //иначе - до координат предполагаемого нахождения цели.	 
            lastDetectedInfo = (CamArray[CamIndex] as IMyCameraBlock).Raycast(correctedTargetLocation);
          //если что-то нашли лучем, то захват обновлен	 
          if ((!lastDetectedInfo.IsEmpty()) && (lastDetectedInfo.Relationship != MyRelationsBetweenPlayerAndBlock.Owner))
          {
            Locked = true;
            CurrentTarget = lastDetectedInfo;
            LastLockTick = Tick;
            IMyTextPanel LCD = gts.GetBlockWithName("LCD") as IMyTextPanel;
            TicksPassed = 0;
          }
          else //иначе - захват потерян 
          {
            Locked = false;
            //CurrentTarget = lastDetectedInfo;
          }
        }
      }

      //этот метод сбрасывает захват цели 
      public void StopLock()
      {
        CurrentTarget = (CamArray[0] as IMyCameraBlock).Raycast(0, 0, 0);
      }

      // этот метод выводит данные по захваченному объекту на панель 

      public void Update()
      {
        MyPos = CamArray[0].GetPosition();
        //если в захвате находится какой-то объект, выполняем следующие действия 
        if (CurrentTarget.EntityId != 0)
        {
          TicksPassed = Tick - LastLockTick;
          //считаем предполагаемые координаты цели (прежние координаты + вектор скорости * прошедшее время с последнего обновления захвата) 
          if (CENTER_SHOT)
          {
            correctedTargetLocation = CurrentTarget.Position + (CurrentTarget.Velocity * TicksPassed / 60);
          }
          else
          {
            T = GridToWorld(O, CurrentTarget.Position, CurrentTarget.Orientation);
            correctedTargetLocation = T + (CurrentTarget.Velocity * TicksPassed / 60);
          }
          // добавим к дистанции до объекта 10 м (так просто для надежности) 
          TargetDistance = (correctedTargetLocation - MyPos).Length() + 10;

          //дальнейшее выполняется в случае, если пришло время обновить захват цели. Частота захвата в тиках считается как дистанция до объекта / 2000 * 60 / кол-во камер в массиве 
          // 2000 - это скорость восстановления дальности raycast по умолчанию) 
          // на 60 умножаем т.к. 2000 восстанавливается в сек, а в 1 сек 60 программных тиков 
          if (TicksPassed > TargetDistance * 0.03 / CamArray.Count)
          {
            Lock();
          }
        }
      }
    }

    public static Vector3D GridToWorld(Vector3 position, Vector3 GridPosition, MatrixD matrix)
    {
      double num1 = (position.X * matrix.M11 + position.Y * matrix.M21 + position.Z * matrix.M31);
      double num2 = (position.X * matrix.M12 + position.Y * matrix.M22 + position.Z * matrix.M32);
      double num3 = (position.X * matrix.M13 + position.Y * matrix.M23 + position.Z * matrix.M33);
      return new Vector3D(num1, num2, num3) + GridPosition;
    }
    public static Vector3D WorldToGrid(Vector3 world_position, Vector3 GridPosition, MatrixD matrix)
    {
      Vector3D position = world_position - GridPosition;
      double num1 = (position.X * matrix.M11 + position.Y * matrix.M12 + position.Z * matrix.M13);
      double num2 = (position.X * matrix.M21 + position.Y * matrix.M22 + position.Z * matrix.M23);
      double num3 = (position.X * matrix.M31 + position.Y * matrix.M32 + position.Z * matrix.M33);
      return new Vector3D(num1, num2, num3);
    }
    public static float TrimF(float Value, float Max, float Min)
    {
      return Math.Min(Math.Max(Value, Min), Max);
    }

    public static int VerticalDelay(float S, float T, int Delay0)
    {
      float V2 = Delay0 * MAX_VELOCITY / 60;
      float Tsqr = T * T;
      float Ssqr = S * S;
      float Vsqr = V2 * V2;
      float H = (float)Math.Sqrt(Ssqr + Vsqr);
      return (int)((-Tsqr * H
      - 2 * T * V2 * H + 2 * Ssqr * (T + V2) - Tsqr * T
      - 3 * Tsqr * V2 - 2 * T * Vsqr) / (2 * (Ssqr - Tsqr - 2 * T * V2) * (MAX_VELOCITY / 60)));
    }

    #region PreludeFooter
  }
}
#endregion