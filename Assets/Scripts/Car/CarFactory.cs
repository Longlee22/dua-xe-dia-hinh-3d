using UnityEngine;

// Dựng xe 3D chi tiết bằng mesh sinh tự động: thân bo cạnh, kính, khung ống, lốp gai, vành nan hoa...
// Vật lý (WheelCollider/Rigidbody) giữ nguyên như bản cũ.
public static class CarFactory
{
    struct Spec
    {
        public string name; public Vector3 body; public float radius, width, clearance, mass, torque, steer, maxKmh;
        public Color color; public Color rimColor; public bool cabin;
    }

    static readonly Spec[] specs =
    {
        new Spec{ name="Jeep",          body=new Vector3(1.8f,0.8f,3.6f), radius=0.40f, width=0.30f, clearance=0.35f, mass=1400f, torque=700f,  steer=32f, maxKmh=90f,  color=new Color(0.30f,0.40f,0.22f), rimColor=new Color(0.82f,0.83f,0.86f), cabin=true  },
        new Spec{ name="Buggy",         body=new Vector3(1.6f,0.5f,3.0f), radius=0.35f, width=0.28f, clearance=0.30f, mass=900f,  torque=520f,  steer=36f, maxKmh=110f, color=new Color(0.95f,0.45f,0.06f), rimColor=new Color(0.35f,0.36f,0.40f), cabin=false },
        new Spec{ name="Monster Truck", body=new Vector3(2.2f,1.1f,4.0f), radius=0.70f, width=0.55f, clearance=0.80f, mass=2200f, torque=1200f, steer=28f, maxKmh=80f,  color=new Color(0.72f,0.10f,0.10f), rimColor=new Color(0.88f,0.89f,0.92f), cabin=true  },
    };
    static readonly Color[] driverColors = { new Color(0.15f,0.35f,0.85f), new Color(0.95f,0.78f,0.10f), new Color(0.90f,0.25f,0.55f) };

    static Texture2D softCircle;

    // ---------- Tiện ích dựng hình ----------
    static GameObject Part(Mesh mesh, Material mat, Transform parent, string name, Vector3 pos, Vector3 scale, Quaternion rot)
    {
        var go = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
        go.transform.SetParent(parent, false);
        go.transform.localPosition = pos;
        go.transform.localRotation = rot;
        go.transform.localScale = scale;
        go.GetComponent<MeshFilter>().sharedMesh = mesh;
        go.GetComponent<MeshRenderer>().sharedMaterial = mat;
        return go;
    }

    static GameObject Part(Mesh mesh, Material mat, Transform parent, string name, Vector3 pos, Vector3 scale)
    {
        return Part(mesh, mat, parent, name, pos, scale, Quaternion.identity);
    }

    static GameObject Box(Transform parent, string name, Vector3 size, float bevel, Material mat, Vector3 pos)
    {
        return Part(CarMesh.BeveledBox(size, bevel), mat, parent, name, pos, Vector3.one);
    }

    static GameObject Box(Transform parent, string name, Vector3 size, float bevel, Material mat, Vector3 pos, Quaternion rot)
    {
        return Part(CarMesh.BeveledBox(size, bevel), mat, parent, name, pos, Vector3.one, rot);
    }

    // Ống nối 2 điểm (khung, trục, ống xả...)
    static void Tube(Transform parent, string name, Vector3 a, Vector3 b, float diameter, Material mat, int seg = 10)
    {
        Vector3 d = b - a;
        float len = d.magnitude;
        if (len < 0.001f) return;
        Part(CarMesh.Cylinder(seg), mat, parent, name, (a + b) * 0.5f, new Vector3(diameter, len, diameter),
            Quaternion.FromToRotation(Vector3.up, d / len));
    }

    static GameObject Disc(Transform parent, string name, float diameter, float thickness, Material mat, Vector3 pos, Quaternion rot)
    {
        return Part(CarMesh.Cylinder(18), mat, parent, name, pos, new Vector3(diameter, thickness, diameter), rot);
    }

    static Quaternion AxisZ { get { return Quaternion.Euler(90f, 0f, 0f); } }
    static Quaternion AxisX { get { return Quaternion.Euler(0f, 0f, 90f); } }

    static Texture2D SoftCircle()
    {
        if (softCircle != null) return softCircle;
        int n = 32;
        softCircle = new Texture2D(n, n, TextureFormat.RGBA32, false);
        for (int y = 0; y < n; y++)
            for (int x = 0; x < n; x++)
            {
                float d = Vector2.Distance(new Vector2(x, y), new Vector2(n / 2f, n / 2f)) / (n / 2f);
                softCircle.SetPixel(x, y, new Color(1, 1, 1, Mathf.Clamp01(1f - d)));
            }
        softCircle.Apply();
        return softCircle;
    }

    // ---------- Tạo xe ----------
    public static CarController Create(int type, int driver, bool isPlayer, int terrain, Vector3 pos, Quaternion rot, string racerName)
    {
        type = Mathf.Clamp(type, 0, specs.Length - 1);
        var s = specs[type];
        var root = new GameObject((isPlayer ? "PlayerCar_" : "AICar_") + s.name);
        root.transform.SetPositionAndRotation(pos, rot);

        var rb = root.AddComponent<Rigidbody>();
        rb.mass = s.mass;
        rb.interpolation = RigidbodyInterpolation.Interpolate;
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        float bodyY = -s.radius + s.clearance + s.body.y / 2f;
        var bc = root.AddComponent<BoxCollider>();
        bc.center = new Vector3(0, bodyY, 0);
        bc.size = s.body;

        var car = root.AddComponent<CarController>();
        car.carName = s.name; car.motorTorque = s.torque; car.maxSteer = s.steer; car.maxSpeedKmh = s.maxKmh;
        car.isPlayer = isPlayer; car.racerName = racerName;

        var v = new GameObject("Visual").transform;
        v.SetParent(root.transform, false);

        switch (type)
        {
            case 0: BuildJeep(v, s, driver); break;
            case 1: BuildBuggy(v, s, driver); break;
            default: BuildMonster(v, s, driver); break;
        }

        // Bánh xe: FL, FR, RL, RR
        float wx = s.body.x / 2f + 0.05f, wz = s.body.z * 0.32f;
        Vector3[] wp = { new Vector3(-wx, 0, wz), new Vector3(wx, 0, wz), new Vector3(-wx, 0, -wz), new Vector3(wx, 0, -wz) };
        for (int i = 0; i < 4; i++)
        {
            var wcGo = new GameObject("WheelCollider_" + i);
            wcGo.transform.SetParent(root.transform, false);
            wcGo.transform.localPosition = wp[i];
            var wc = wcGo.AddComponent<WheelCollider>();
            wc.radius = s.radius; wc.mass = 25f; wc.suspensionDistance = 0.35f;
            var sp = wc.suspensionSpring; sp.spring = s.mass * 22f; sp.damper = s.mass * 2.8f; sp.targetPosition = 0.5f;
            wc.suspensionSpring = sp;
            car.wheels[i] = wc;

            var holder = new GameObject("Wheel_" + i);
            holder.transform.SetParent(root.transform, false);
            holder.transform.localPosition = wp[i];
            float width = s.width * (type == 1 && i >= 2 ? 1.12f : 1f);
            BuildWheel(holder.transform, s.radius, width, i % 2 == 0 ? -1 : 1, s.rimColor, type == 2);
            car.wheelMeshes[i] = holder.transform;
        }

        // Âm thanh động cơ
        var eng = new GameObject("EngineSound");
        eng.transform.SetParent(root.transform, false);
        var src = eng.AddComponent<AudioSource>();
        src.clip = AudioFactory.Engine();
        src.loop = true; src.playOnAwake = false;
        src.spatialBlend = isPlayer ? 0f : 1f;
        src.minDistance = 6f; src.maxDistance = 90f;
        src.volume = 0.4f;
        car.engineSource = src;

        // Hiệu ứng bụi
        var dGo = new GameObject("Dust");
        dGo.transform.SetParent(root.transform, false);
        dGo.transform.localPosition = new Vector3(0, -s.radius + 0.25f, -s.body.z * 0.5f);
        dGo.transform.localRotation = Quaternion.Euler(-90, 0, 0);
        var ps = dGo.AddComponent<ParticleSystem>();
        ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        var main = ps.main;
        main.simulationSpace = ParticleSystemSimulationSpace.World;
        main.startLifetime = 1.3f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 2.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.8f, 2.2f);
        main.startColor = GameData.DustColors[Mathf.Clamp(terrain, 0, 3)];
        main.maxParticles = 400;
        main.loop = true;
        var em = ps.emission; em.rateOverTime = 0f;
        var sh = ps.shape; sh.shapeType = ParticleSystemShapeType.Cone; sh.angle = 25f; sh.radius = 0.5f;
        var col = ps.colorOverLifetime; col.enabled = true;
        var grad = new Gradient();
        grad.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                     new[] { new GradientAlphaKey(0.9f, 0f), new GradientAlphaKey(0f, 1f) });
        col.color = grad;
        var szl = ps.sizeOverLifetime; szl.enabled = true;
        szl.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 0.5f, 1f, 1.8f));
        var rend = dGo.GetComponent<ParticleSystemRenderer>();
        rend.sharedMaterial = MatHelper.MakeParticle(SoftCircle());
        ps.Play();
        car.dust = ps;

        car.Setup(GameData.TerrainGrip[Mathf.Clamp(terrain, 0, 3)]);
        return car;
    }

    // ---------- Bánh xe ----------
    static void BuildWheel(Transform holder, float r, float width, int side, Color rimColor, bool monster)
    {
        var rubber = MatHelper.MakeRubber();
        var rim = MatHelper.MakeMetal(rimColor, 0.75f);
        var dark = MatHelper.MakeMetal(new Color(0.16f, 0.17f, 0.19f), 0.4f);

        Part(CarMesh.Tire(24), rubber, holder, "Tire", Vector3.zero, new Vector3(r * 2f, width, r * 2f), AxisX);

        float rimR = r * 0.56f;
        Part(CarMesh.Cylinder(18), dark, holder, "RimBarrel", Vector3.zero, new Vector3(rimR * 2f, width * 1.01f, rimR * 2f), AxisX);
        float faceX = side * (width * 0.5f + 0.012f);
        Disc(holder, "RimFace", rimR * 1.94f, 0.03f, rim, new Vector3(faceX, 0, 0), AxisX);

        int spokes = monster ? 6 : 5;
        for (int i = 0; i < spokes; i++)
        {
            float a = i * Mathf.PI * 2f / spokes;
            Part(CarMesh.BeveledBox(new Vector3(0.05f, rimR * 0.86f, rimR * 0.24f), 0.012f), rim, holder,
                "Spoke" + i,
                new Vector3(faceX + side * 0.015f, Mathf.Cos(a) * rimR * 0.42f, Mathf.Sin(a) * rimR * 0.42f),
                Vector3.one, Quaternion.Euler(a * Mathf.Rad2Deg, 0f, 0f));
        }
        Disc(holder, "Hub", rimR * 0.34f, 0.05f, MatHelper.MakeChrome(), new Vector3(faceX + side * 0.03f, 0, 0), AxisX);
        Disc(holder, "BrakeDisc", rimR * 1.5f, 0.02f, dark, new Vector3(-side * 0.02f, 0, 0), AxisX);
        if (monster)
            Disc(holder, "BeadLock", rimR * 2.06f, 0.05f, MatHelper.MakeChrome(), new Vector3(faceX + side * 0.04f, 0, 0), AxisX);
    }

    // ---------- Tay đua ----------
    public static GameObject BuildDriver(Transform parent, Vector3 pos, float rotY, Color suit, bool helmetOnly = false)
    {
        var d = new GameObject("Driver").transform;
        d.SetParent(parent, false);
        d.localPosition = pos;
        d.localRotation = Quaternion.Euler(0f, rotY, 0f);

        var cloth = MatHelper.MakePaint(suit);
        var clothDark = MatHelper.MakePlastic(suit * 0.55f, 0.4f);
        var skin = MatHelper.MakePlastic(new Color(0.85f, 0.66f, 0.52f), 0.45f);
        var black = MatHelper.MakePlastic(new Color(0.08f, 0.08f, 0.09f), 0.5f);
        var chrome = MatHelper.MakeChrome();

        // Thân + vai
        Box(d, "Torso", new Vector3(0.36f, 0.46f, 0.26f), 0.07f, cloth, new Vector3(0f, 0.30f, 0f));
        Box(d, "Chest", new Vector3(0.40f, 0.14f, 0.28f), 0.04f, clothDark, new Vector3(0f, 0.47f, 0.01f));
        Box(d, "StripeL", new Vector3(0.06f, 0.40f, 0.02f), 0.01f, MatHelper.MakePaint(Color.white), new Vector3(-0.10f, 0.34f, 0.135f));
        Box(d, "StripeR", new Vector3(0.06f, 0.40f, 0.02f), 0.01f, MatHelper.MakePaint(Color.white), new Vector3(0.10f, 0.34f, 0.135f));

        // Cổ + đầu
        Tube(d, "Neck", new Vector3(0f, 0.50f, 0f), new Vector3(0f, 0.60f, 0.01f), 0.14f, skin);
        Part(CarMesh.Sphere(16, 12), skin, d, "Head", new Vector3(0f, 0.68f, 0.01f), Vector3.one * 0.42f);

        // Mũ bảo hiểm (cùng màu bộ đồ, sọc trắng, kính đen)
        Part(CarMesh.Sphere(16, 12), MatHelper.MakePaint(suit), d, "Helmet", new Vector3(0f, 0.71f, 0.005f), Vector3.one * 0.50f);
        Box(d, "Visor", new Vector3(0.28f, 0.10f, 0.06f), 0.025f, MatHelper.MakeGlass(new Color(0.04f, 0.06f, 0.10f, 0.90f)), new Vector3(0f, 0.72f, 0.225f));
        Box(d, "HelmetStripe", new Vector3(0.06f, 0.03f, 0.34f), 0.01f, MatHelper.MakePaint(Color.white), new Vector3(0f, 0.948f, 0f));

        // Tay lái
        Tube(d, "ArmL", new Vector3(-0.20f, 0.47f, 0.02f), new Vector3(-0.16f, 0.26f, 0.30f), 0.14f, cloth);
        Tube(d, "ArmR", new Vector3(0.20f, 0.47f, 0.02f), new Vector3(0.16f, 0.26f, 0.30f), 0.14f, cloth);
        Part(CarMesh.Sphere(12, 8), black, d, "GloveL", new Vector3(-0.16f, 0.25f, 0.33f), Vector3.one * 0.17f);
        Part(CarMesh.Sphere(12, 8), black, d, "GloveR", new Vector3(0.16f, 0.25f, 0.33f), Vector3.one * 0.17f);

        // Dây an toàn
        Box(d, "Belt", new Vector3(0.34f, 0.05f, 0.02f), 0.01f, black, new Vector3(0f, 0.26f, 0.135f), Quaternion.Euler(0, 0, 18f));
        Box(d, "SteeringColumn", new Vector3(0.05f, 0.05f, 0.30f), 0.015f, chrome, new Vector3(0f, 0.22f, 0.30f), Quaternion.Euler(-65f, 0, 0));
        return d.gameObject;
    }

    static void BuildSeat(Transform parent, string name, Vector3 pos, Color seatColor)
    {
        var fabric = MatHelper.MakePlastic(seatColor, 0.3f);
        var frame = MatHelper.MakeMetal(new Color(0.12f, 0.12f, 0.13f), 0.45f);
        Box(parent, name + "_Base", new Vector3(0.55f, 0.14f, 0.55f), 0.04f, fabric, pos);
        Box(parent, name + "_Back", new Vector3(0.55f, 0.62f, 0.14f), 0.05f, fabric, pos + new Vector3(0f, 0.34f, -0.30f), Quaternion.Euler(-9f, 0, 0));
        Box(parent, name + "_Head", new Vector3(0.32f, 0.24f, 0.13f), 0.05f, fabric, pos + new Vector3(0f, 0.72f, -0.34f), Quaternion.Euler(-9f, 0, 0));
        Tube(parent, name + "_Rail", pos + new Vector3(-0.22f, -0.08f, 0f), pos + new Vector3(-0.22f, -0.08f, 0f) + Vector3.back * 0.4f, 0.05f, frame);
    }

    static void SteeringWheel(Transform parent, Vector3 pos)
    {
        var black = MatHelper.MakePlastic(new Color(0.07f, 0.07f, 0.08f), 0.55f);
        Part(CarMesh.Cylinder(18), black, parent, "SteeringWheel", pos, new Vector3(0.34f, 0.045f, 0.34f), Quaternion.Euler(66f, 0, 0));
        Part(CarMesh.Sphere(10, 8), MatHelper.MakeMetal(new Color(0.6f, 0.6f, 0.62f), 0.6f), parent, "SteeringHub", pos, Vector3.one * 0.09f);
    }

    // ---------- Dựng tay đua riêng để chụp ảnh chọn nhân vật ----------
    public static GameObject CreateDriverPreview(int driver)
    {
        var root = new GameObject("DriverPreview");
        var seatColor = new Color(0.13f, 0.13f, 0.14f);
        BuildSeat(root.transform, "Seat", Vector3.zero, seatColor);
        BuildDriver(root.transform, new Vector3(0f, 0.04f, 0.02f), 0f, driverColors[Mathf.Clamp(driver, 0, 2)]);
        SteeringWheel(root.transform, new Vector3(0f, 0.30f, 0.54f));
        return root;
    }

    // ---------- JEEP ----------
    static void BuildJeep(Transform v, Spec s, int driver)
    {
        var paint = MatHelper.MakePaint(s.color);
        var paintDark = MatHelper.MakePaint(s.color * 0.78f);
        var black = MatHelper.MakePlastic(new Color(0.06f, 0.06f, 0.07f), 0.4f);
        var frame = MatHelper.MakeMetal(new Color(0.13f, 0.13f, 0.14f), 0.45f);
        var chrome = MatHelper.MakeChrome();
        var glass = MatHelper.MakeGlass(new Color(0.35f, 0.55f, 0.65f, 0.42f));
        var lamp = MatHelper.MakeEmissive(new Color(1f, 0.94f, 0.75f), 2.2f);
        var tail = MatHelper.MakeEmissive(new Color(1f, 0.10f, 0.05f), 1.8f);
        var amber = MatHelper.MakeEmissive(new Color(1f, 0.55f, 0.05f), 1.8f);

        // Khung gầm
        Box(v, "RailL", new Vector3(0.14f, 0.12f, 3.30f), 0.03f, frame, new Vector3(-0.55f, -0.03f, 0f));
        Box(v, "RailR", new Vector3(0.14f, 0.12f, 3.30f), 0.03f, frame, new Vector3(0.55f, -0.03f, 0f));
        for (int i = 0; i < 4; i++)
            Box(v, "Cross" + i, new Vector3(1.20f, 0.10f, 0.12f), 0.02f, frame, new Vector3(0f, -0.03f, -1.4f + i * 0.95f));
        Box(v, "Skid", new Vector3(1.10f, 0.05f, 1.00f), 0.02f, frame, new Vector3(0f, -0.12f, 0.60f));

        // Thân + nắp capo + khoang sau
        Box(v, "Tub", new Vector3(1.72f, 0.58f, 2.50f), 0.10f, paint, new Vector3(0f, 0.36f, -0.30f));
        Box(v, "Hood", new Vector3(1.62f, 0.20f, 1.30f), 0.06f, paint, new Vector3(0f, 0.68f, 0.96f), Quaternion.Euler(-2f, 0f, 0f));
        Box(v, "HoodVent", new Vector3(0.55f, 0.03f, 0.55f), 0.01f, paintDark, new Vector3(0f, 0.785f, 0.95f));
        Box(v, "BedFloor", new Vector3(1.52f, 0.06f, 0.85f), 0.02f, black, new Vector3(0f, 0.65f, -1.24f));
        Box(v, "RearPanel", new Vector3(1.70f, 0.52f, 0.10f), 0.03f, paint, new Vector3(0f, 0.42f, -1.60f));
        Box(v, "Tailgate", new Vector3(1.30f, 0.40f, 0.05f), 0.02f, paintDark, new Vector3(0f, 0.44f, -1.66f));

        // Mặt ca-lăng + đèn
        Box(v, "Grille", new Vector3(1.44f, 0.46f, 0.10f), 0.03f, black, new Vector3(0f, 0.50f, 1.63f));
        for (int i = 0; i < 7; i++)
            Box(v, "GrilleBar" + i, new Vector3(0.055f, 0.34f, 0.05f), 0.01f, paintDark, new Vector3(-0.42f + i * 0.14f, 0.50f, 1.69f));
        Disc(v, "HeadlightL", 0.26f, 0.06f, lamp, new Vector3(-0.60f, 0.62f, 1.70f), AxisZ);
        Disc(v, "HeadlightR", 0.26f, 0.06f, lamp, new Vector3(0.60f, 0.62f, 1.70f), AxisZ);
        Disc(v, "HeadlightRingL", 0.32f, 0.03f, chrome, new Vector3(-0.60f, 0.62f, 1.66f), AxisZ);
        Disc(v, "HeadlightRingR", 0.32f, 0.03f, chrome, new Vector3(0.60f, 0.62f, 1.66f), AxisZ);
        Box(v, "IndicatorL", new Vector3(0.16f, 0.07f, 0.05f), 0.01f, amber, new Vector3(-0.60f, 0.38f, 1.70f));
        Box(v, "IndicatorR", new Vector3(0.16f, 0.07f, 0.05f), 0.01f, amber, new Vector3(0.60f, 0.38f, 1.70f));

        // Cản trước + tời
        Box(v, "BumperF", new Vector3(1.92f, 0.16f, 0.18f), 0.04f, frame, new Vector3(0f, 0.20f, 1.80f));
        Box(v, "BumperFL", new Vector3(0.10f, 0.34f, 0.14f), 0.02f, frame, new Vector3(-0.62f, 0.30f, 1.82f));
        Box(v, "BumperFR", new Vector3(0.10f, 0.34f, 0.14f), 0.02f, frame, new Vector3(0.62f, 0.30f, 1.82f));
        Disc(v, "Winch", 0.24f, 0.34f, chrome, new Vector3(0f, 0.36f, 1.80f), AxisX);
        Box(v, "Plate", new Vector3(0.50f, 0.14f, 0.03f), 0.01f, MatHelper.MakePlastic(new Color(0.85f, 0.85f, 0.8f), 0.4f), new Vector3(0f, 0.24f, 1.90f));

        // Chắn bùn 4 vòng cung
        var flare = MatHelper.MakePlastic(new Color(0.09f, 0.09f, 0.10f), 0.35f);
        float wz = s.body.z * 0.32f;
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -1f : 1f) * (s.body.x * 0.5f + 0.07f);
            float z = (i < 2 ? 1f : -1f) * wz;
            Part(CarMesh.Arch(0.46f, 0.60f, 0.30f, 10f, 170f, 14), flare, v, "Fender" + i, new Vector3(x, 0f, z), Vector3.one);
        }

        // Bậc lên xuống + ốp hông
        for (int side = -1; side <= 1; side += 2)
        {
            Tube(v, "Step" + side, new Vector3(side * 0.90f, 0.06f, -0.95f), new Vector3(side * 0.90f, 0.06f, 0.95f), 0.10f, frame);
            Box(v, "Rocker" + side, new Vector3(0.10f, 0.18f, 1.55f), 0.03f, black, new Vector3(side * 0.86f, 0.12f, 0f));
        }

        // Kính chắn gió + khung
        Box(v, "Windshield", new Vector3(1.30f, 0.62f, 0.02f), 0.01f, glass, new Vector3(0f, 1.02f, 0.70f), Quaternion.Euler(-10f, 0f, 0f));
        Tube(v, "WS_L", new Vector3(-0.66f, 0.66f, 0.75f), new Vector3(-0.66f, 1.34f, 0.60f), 0.06f, black);
        Tube(v, "WS_R", new Vector3(0.66f, 0.66f, 0.75f), new Vector3(0.66f, 1.34f, 0.60f), 0.06f, black);
        Tube(v, "WS_Top", new Vector3(-0.66f, 1.34f, 0.60f), new Vector3(0.66f, 1.34f, 0.60f), 0.06f, black);

        // Lồng bảo vệ
        Tube(v, "CageL1", new Vector3(-0.70f, 0.64f, -0.95f), new Vector3(-0.70f, 1.52f, -0.95f), 0.07f, black);
        Tube(v, "CageR1", new Vector3(0.70f, 0.64f, -0.95f), new Vector3(0.70f, 1.52f, -0.95f), 0.07f, black);
        Tube(v, "CageTop", new Vector3(-0.70f, 1.52f, -0.95f), new Vector3(0.70f, 1.52f, -0.95f), 0.07f, black);
        Tube(v, "CageL2", new Vector3(-0.70f, 1.52f, -0.95f), new Vector3(-0.66f, 1.34f, 0.60f), 0.06f, black);
        Tube(v, "CageR2", new Vector3(0.70f, 1.52f, -0.95f), new Vector3(0.66f, 1.34f, 0.60f), 0.06f, black);
        Tube(v, "CageBraceL", new Vector3(-0.70f, 1.52f, -0.95f), new Vector3(-0.58f, 0.70f, -1.60f), 0.06f, black);
        Tube(v, "CageBraceR", new Vector3(0.70f, 1.52f, -0.95f), new Vector3(0.58f, 0.70f, -1.60f), 0.06f, black);
        Tube(v, "CageX", new Vector3(-0.70f, 1.10f, -0.95f), new Vector3(0.70f, 1.52f, -0.95f), 0.045f, black);

        // Ghế + táp-lô + vô-lăng
        var seatColor = new Color(0.16f, 0.16f, 0.17f);
        BuildSeat(v, "SeatDriver", new Vector3(-0.38f, 0.66f, -0.35f), seatColor);
        BuildSeat(v, "SeatPass", new Vector3(0.38f, 0.66f, -0.35f), seatColor);
        Box(v, "Dash", new Vector3(1.48f, 0.18f, 0.30f), 0.04f, black, new Vector3(0f, 0.82f, 0.42f));
        Disc(v, "Gauge1", 0.14f, 0.03f, MatHelper.MakeEmissive(new Color(0.4f, 0.85f, 1f), 0.7f), new Vector3(-0.38f, 0.90f, 0.56f), Quaternion.Euler(100f, 0f, 0f));
        SteeringWheel(v, new Vector3(-0.38f, 0.98f, 0.20f));

        BuildDriver(v, new Vector3(-0.38f, 0.74f, -0.30f), 0f, driverColors[Mathf.Clamp(driver, 0, 2)]);

        // Đuôi: đèn, cản, lốp dự phòng, ống xả
        Box(v, "TailL", new Vector3(0.18f, 0.16f, 0.05f), 0.01f, tail, new Vector3(-0.68f, 0.70f, -1.65f));
        Box(v, "TailR", new Vector3(0.18f, 0.16f, 0.05f), 0.01f, tail, new Vector3(0.68f, 0.70f, -1.65f));
        Box(v, "BumperR", new Vector3(1.90f, 0.16f, 0.18f), 0.04f, frame, new Vector3(0f, 0.22f, -1.74f));
        Part(CarMesh.Tire(24), MatHelper.MakeRubber(), v, "SpareTire", new Vector3(0f, 0.66f, -1.80f), new Vector3(0.66f, 0.22f, 0.66f), AxisZ);
        Disc(v, "SpareRim", 0.36f, 0.03f, chrome, new Vector3(0f, 0.66f, -1.91f), AxisZ);
        Tube(v, "Exhaust", new Vector3(0.45f, 0.05f, -1.55f), new Vector3(0.55f, 0.10f, -1.92f), 0.08f, chrome);

        // Gương, ống thở, ăng-ten
        for (int side = -1; side <= 1; side += 2)
        {
            Tube(v, "MirrorArm" + side, new Vector3(side * 0.66f, 1.05f, 0.72f), new Vector3(side * 0.90f, 1.10f, 0.80f), 0.035f, black);
            Box(v, "Mirror" + side, new Vector3(0.06f, 0.14f, 0.20f), 0.02f, black, new Vector3(side * 0.93f, 1.10f, 0.80f));
            Disc(v, "MirrorGlass" + side, 0.16f, 0.02f, chrome, new Vector3(side * 0.965f, 1.10f, 0.80f), Quaternion.Euler(0f, 90f, 0f));
        }
        Tube(v, "Snorkel", new Vector3(0.80f, 0.45f, 1.02f), new Vector3(0.80f, 1.44f, 0.78f), 0.09f, black);
        Disc(v, "SnorkelHead", 0.16f, 0.10f, black, new Vector3(0.80f, 1.47f, 0.76f), Quaternion.Euler(0f, 90f, 0f));
        Tube(v, "Antenna", new Vector3(-0.72f, 0.70f, -1.45f), new Vector3(-0.76f, 1.85f, -1.50f), 0.02f, chrome);
    }

    // ---------- BUGGY ----------
    static void BuildBuggy(Transform v, Spec s, int driver)
    {
        var paint = MatHelper.MakePaint(s.color);
        var paintDark = MatHelper.MakePaint(s.color * 0.72f);
        var black = MatHelper.MakePlastic(new Color(0.06f, 0.06f, 0.07f), 0.4f);
        var tube = MatHelper.MakeMetal(new Color(0.20f, 0.21f, 0.24f), 0.65f);
        var chrome = MatHelper.MakeChrome();
        var glass = MatHelper.MakeGlass(new Color(0.30f, 0.50f, 0.60f, 0.35f));
        var lamp = MatHelper.MakeEmissive(new Color(1f, 0.95f, 0.8f), 2.2f);

        // Khung ống chịu lực
        for (int side = -1; side <= 1; side += 2)
        {
            Tube(v, "LowerRail" + side, new Vector3(side * 0.62f, 0.26f, 1.35f), new Vector3(side * 0.62f, 0.24f, -1.45f), 0.09f, tube);
            Tube(v, "UpperRail" + side, new Vector3(side * 0.62f, 0.52f, 0.55f), new Vector3(side * 0.62f, 0.50f, -1.25f), 0.08f, tube);
            Tube(v, "Diag" + side, new Vector3(side * 0.62f, 0.26f, 0.55f), new Vector3(side * 0.62f, 0.52f, 0.10f), 0.06f, tube);
        }
        for (int i = 0; i < 3; i++)
            Tube(v, "Cross" + i, new Vector3(-0.62f, 0.26f + i * 0.06f, 1.15f - i * 1.1f), new Vector3(0.62f, 0.26f + i * 0.06f, 1.15f - i * 1.1f), 0.07f, tube);

        // Vỏ xe
        Box(v, "Nose", new Vector3(1.36f, 0.30f, 1.15f), 0.12f, paint, new Vector3(0f, 0.30f, 0.92f), Quaternion.Euler(7f, 0f, 0f));
        Box(v, "Cockpit", new Vector3(1.24f, 0.34f, 1.15f), 0.10f, paint, new Vector3(0f, 0.32f, -0.18f));
        Box(v, "EngineCover", new Vector3(1.28f, 0.44f, 0.80f), 0.10f, paintDark, new Vector3(0f, 0.44f, -1.22f));
        Box(v, "NoseStripe", new Vector3(0.42f, 0.02f, 1.10f), 0.01f, MatHelper.MakePaint(Color.white), new Vector3(0f, 0.455f, 0.92f), Quaternion.Euler(7f, 0f, 0f));

        // Động cơ sau
        Disc(v, "AirFilter", 0.34f, 0.16f, chrome, new Vector3(0f, 0.70f, -1.18f), Quaternion.Euler(0f, 0f, 15f));
        Box(v, "EngineBlock", new Vector3(0.65f, 0.30f, 0.45f), 0.05f, MatHelper.MakeMetal(new Color(0.25f, 0.26f, 0.28f), 0.6f), new Vector3(0f, 0.66f, -1.20f));
        Tube(v, "ExhaustL", new Vector3(-0.30f, 0.55f, -1.55f), new Vector3(-0.40f, 1.02f, -1.70f), 0.10f, chrome);
        Tube(v, "ExhaustR", new Vector3(0.30f, 0.55f, -1.55f), new Vector3(0.40f, 1.02f, -1.70f), 0.10f, chrome);

        // Lồng bảo vệ
        Tube(v, "CageL1", new Vector3(-0.60f, 0.42f, -0.65f), new Vector3(-0.55f, 1.58f, -0.60f), 0.08f, tube);
        Tube(v, "CageR1", new Vector3(0.60f, 0.42f, -0.65f), new Vector3(0.55f, 1.58f, -0.60f), 0.08f, tube);
        Tube(v, "CageTop", new Vector3(-0.55f, 1.58f, -0.60f), new Vector3(0.55f, 1.58f, -0.60f), 0.08f, tube);
        Tube(v, "CageFrontL", new Vector3(-0.55f, 1.58f, -0.60f), new Vector3(-0.58f, 1.05f, 0.55f), 0.07f, tube);
        Tube(v, "CageFrontR", new Vector3(0.55f, 1.58f, -0.60f), new Vector3(0.58f, 1.05f, 0.55f), 0.07f, tube);
        Tube(v, "CageFrontTop", new Vector3(-0.58f, 1.05f, 0.55f), new Vector3(0.58f, 1.05f, 0.55f), 0.07f, tube);
        Tube(v, "CageBackL", new Vector3(-0.55f, 1.58f, -0.60f), new Vector3(-0.58f, 0.42f, -1.30f), 0.07f, tube);
        Tube(v, "CageBackR", new Vector3(0.55f, 1.58f, -0.60f), new Vector3(0.58f, 0.42f, -1.30f), 0.07f, tube);

        // Kính chắn gió nhỏ
        Box(v, "Windshield", new Vector3(1.10f, 0.46f, 0.02f), 0.01f, glass, new Vector3(0f, 1.02f, 0.58f), Quaternion.Euler(-18f, 0f, 0f));

        // Ghế, vô-lăng, tay đua
        BuildSeat(v, "SeatDriver", new Vector3(-0.30f, 0.44f, -0.35f), new Color(0.12f, 0.12f, 0.13f));
        BuildSeat(v, "SeatPass", new Vector3(0.30f, 0.44f, -0.35f), new Color(0.12f, 0.12f, 0.13f));
        SteeringWheel(v, new Vector3(-0.30f, 0.78f, 0.18f));
        BuildDriver(v, new Vector3(-0.30f, 0.50f, -0.32f), 0f, driverColors[Mathf.Clamp(driver, 0, 2)]);

        // Cản trước + đèn
        Tube(v, "BumperF", new Vector3(-0.72f, 0.42f, 1.48f), new Vector3(0.72f, 0.42f, 1.48f), 0.10f, tube);
        Tube(v, "BumperF2", new Vector3(-0.72f, 0.26f, 1.42f), new Vector3(0.72f, 0.26f, 1.42f), 0.08f, tube);
        for (int side = -1; side <= 1; side += 2)
        {
            Disc(v, "Light" + side, 0.20f, 0.10f, lamp, new Vector3(side * 0.30f, 0.62f, 1.44f), AxisZ);
            Disc(v, "LightRing" + side, 0.26f, 0.06f, black, new Vector3(side * 0.30f, 0.62f, 1.40f), AxisZ);
        }

        // Giảm xóc lộ thiên (4 bánh)
        float wz = s.body.z * 0.32f;
        for (int i = 0; i < 4; i++)
        {
            float sx = i % 2 == 0 ? -1f : 1f;
            float sz = i < 2 ? 1f : -1f;
            Vector3 top = new Vector3(sx * 0.50f, 0.52f, sz * (wz - 0.15f));
            Vector3 bottom = new Vector3(sx * 0.80f, 0.02f, sz * wz);
            Tube(v, "Shock" + i, top, bottom, 0.07f, chrome);
            Tube(v, "Spring" + i, Vector3.Lerp(top, bottom, 0.25f), Vector3.Lerp(top, bottom, 0.85f), 0.13f, paint);
            Tube(v, "ArmA" + i, new Vector3(sx * 0.55f, 0.24f, sz * (wz - 0.30f)), bottom, 0.07f, tube);
        }

        // Cờ đuôi xe
        Tube(v, "Whip", new Vector3(0.62f, 0.30f, -1.40f), new Vector3(0.72f, 2.05f, -1.52f), 0.025f, black);
        Box(v, "Flag", new Vector3(0.02f, 0.30f, 0.46f), 0.005f, MatHelper.MakePaint(new Color(1f, 0.45f, 0.05f)), new Vector3(0.72f, 1.94f, -1.30f));
    }

    // ---------- MONSTER TRUCK ----------
    static void BuildMonster(Transform v, Spec s, int driver)
    {
        var paint = MatHelper.MakePaint(s.color);
        var paintDark = MatHelper.MakePaint(s.color * 0.72f);
        var black = MatHelper.MakePlastic(new Color(0.05f, 0.05f, 0.06f), 0.4f);
        var frame = MatHelper.MakeMetal(new Color(0.15f, 0.15f, 0.16f), 0.5f);
        var chrome = MatHelper.MakeChrome();
        var glass = MatHelper.MakeGlass(new Color(0.28f, 0.45f, 0.55f, 0.45f));
        var lamp = MatHelper.MakeEmissive(new Color(1f, 0.95f, 0.8f), 2.4f);
        var tail = MatHelper.MakeEmissive(new Color(1f, 0.08f, 0.05f), 1.8f);

        // Khung gầm cao
        Box(v, "RailL", new Vector3(0.20f, 0.16f, 3.70f), 0.03f, frame, new Vector3(-0.60f, 0.02f, 0f));
        Box(v, "RailR", new Vector3(0.20f, 0.16f, 3.70f), 0.03f, frame, new Vector3(0.60f, 0.02f, 0f));
        for (int i = 0; i < 3; i++)
            Box(v, "Cross" + i, new Vector3(1.25f, 0.12f, 0.14f), 0.02f, frame, new Vector3(0f, 0.02f, -1.3f + i * 1.3f));

        // Trục + vi sai
        float wz = s.body.z * 0.32f;
        for (int i = 0; i < 2; i++)
        {
            float z = (i == 0 ? 1f : -1f) * wz;
            Tube(v, "Axle" + i, new Vector3(-1.12f, 0f, z), new Vector3(1.12f, 0f, z), 0.18f, frame);
            Part(CarMesh.Sphere(16, 12), MatHelper.MakeMetal(new Color(0.22f, 0.23f, 0.25f), 0.55f), v, "Diff" + i, new Vector3(0f, 0f, z), new Vector3(0.62f, 0.55f, 0.55f));
            for (int side = -1; side <= 1; side += 2)
            {
                Vector3 top = new Vector3(side * 0.52f, 0.62f, z);
                Vector3 bottom = new Vector3(side * 1.10f, 0.06f, z);
                Tube(v, "Shock" + i + side, top, bottom, 0.09f, chrome);
                Tube(v, "Spring" + i + side, Vector3.Lerp(top, bottom, 0.2f), Vector3.Lerp(top, bottom, 0.8f), 0.17f, paint);
                Tube(v, "Link" + i + side, new Vector3(side * 0.55f, 0.10f, z + (i == 0 ? -0.55f : 0.55f)), bottom, 0.09f, frame);
            }
        }

        // Thân xe: ca-bin + capo + thùng sau (ca-bin mui trần + lồng bảo vệ)
        Box(v, "Cab", new Vector3(2.00f, 0.72f, 1.55f), 0.10f, paint, new Vector3(0f, 0.86f, 0.30f));
        Box(v, "Hood", new Vector3(1.86f, 0.44f, 1.05f), 0.08f, paint, new Vector3(0f, 0.74f, 1.18f), Quaternion.Euler(-2f, 0f, 0f));
        Box(v, "Bed", new Vector3(1.90f, 0.42f, 1.10f), 0.08f, paint, new Vector3(0f, 0.70f, -1.05f));
        Box(v, "BedFloor", new Vector3(1.70f, 0.05f, 0.95f), 0.02f, black, new Vector3(0f, 0.90f, -1.05f));
        Box(v, "BedWallL", new Vector3(0.08f, 0.26f, 0.95f), 0.02f, paint, new Vector3(-0.90f, 1.02f, -1.05f));
        Box(v, "BedWallR", new Vector3(0.08f, 0.26f, 0.95f), 0.02f, paint, new Vector3(0.90f, 1.02f, -1.05f));

        // Kính chắn gió + lồng bảo vệ mui trần
        Box(v, "Windshield", new Vector3(1.58f, 0.40f, 0.02f), 0.01f, glass, new Vector3(0f, 1.03f, 0.98f), Quaternion.Euler(-12f, 0f, 0f));
        Tube(v, "CageL1", new Vector3(-0.88f, 1.16f, 0.55f), new Vector3(-0.84f, 1.60f, 0.40f), 0.075f, black);
        Tube(v, "CageR1", new Vector3(0.88f, 1.16f, 0.55f), new Vector3(0.84f, 1.60f, 0.40f), 0.075f, black);
        Tube(v, "CageL2", new Vector3(-0.88f, 1.16f, -0.38f), new Vector3(-0.84f, 1.60f, -0.30f), 0.075f, black);
        Tube(v, "CageR2", new Vector3(0.88f, 1.16f, -0.38f), new Vector3(0.84f, 1.60f, -0.30f), 0.075f, black);
        Tube(v, "CageFront", new Vector3(-0.84f, 1.60f, 0.40f), new Vector3(0.84f, 1.60f, 0.40f), 0.075f, black);
        Tube(v, "CageBack", new Vector3(-0.84f, 1.60f, -0.30f), new Vector3(0.84f, 1.60f, -0.30f), 0.075f, black);
        Tube(v, "CageTopL", new Vector3(-0.84f, 1.60f, 0.40f), new Vector3(-0.84f, 1.60f, -0.30f), 0.075f, black);
        Tube(v, "CageTopR", new Vector3(0.84f, 1.60f, 0.40f), new Vector3(0.84f, 1.60f, -0.30f), 0.075f, black);

        // Ca-lăng + cản + đèn
        Box(v, "Grille", new Vector3(1.52f, 0.50f, 0.08f), 0.04f, chrome, new Vector3(0f, 0.66f, 1.72f));
        for (int i = 0; i < 5; i++)
            Box(v, "GrilleBar" + i, new Vector3(0.09f, 0.38f, 0.05f), 0.01f, black, new Vector3(-0.44f + i * 0.22f, 0.66f, 1.77f));
        Box(v, "BumperF", new Vector3(2.40f, 0.22f, 0.24f), 0.05f, chrome, new Vector3(0f, 0.34f, 1.94f));
        Box(v, "BumperR", new Vector3(2.34f, 0.20f, 0.22f), 0.05f, chrome, new Vector3(0f, 0.34f, -1.86f));
        Box(v, "TailL", new Vector3(0.16f, 0.24f, 0.06f), 0.02f, tail, new Vector3(-0.82f, 0.78f, -1.62f));
        Box(v, "TailR", new Vector3(0.16f, 0.24f, 0.06f), 0.02f, tail, new Vector3(0.82f, 0.78f, -1.62f));
        Box(v, "Plate", new Vector3(0.46f, 0.14f, 0.03f), 0.01f, MatHelper.MakePlastic(new Color(0.9f, 0.9f, 0.85f), 0.4f), new Vector3(0f, 0.30f, 2.02f));

        // Thanh đèn trên nóc
        Tube(v, "LightBar", new Vector3(-0.86f, 1.38f, 0.82f), new Vector3(0.86f, 1.38f, 0.82f), 0.09f, black);
        for (int i = 0; i < 4; i++)
        {
            float x = -0.60f + i * 0.40f;
            Disc(v, "RoofLight" + i, 0.20f, 0.10f, lamp, new Vector3(x, 1.42f, 0.86f), AxisZ);
            Disc(v, "RoofLightRing" + i, 0.25f, 0.06f, black, new Vector3(x, 1.42f, 0.80f), AxisZ);
        }

        // Ống xả đứng sau ca-bin
        for (int side = -1; side <= 1; side += 2)
        {
            Tube(v, "Stack" + side, new Vector3(side * 0.74f, 0.62f, -0.52f), new Vector3(side * 0.80f, 1.62f, -0.58f), 0.12f, chrome);
            Disc(v, "StackTip" + side, 0.17f, 0.10f, black, new Vector3(side * 0.805f, 1.65f, -0.585f), Quaternion.identity);
        }

        // Vòm chắn bùn
        var flare = MatHelper.MakePlastic(new Color(0.07f, 0.07f, 0.08f), 0.35f);
        for (int i = 0; i < 4; i++)
        {
            float x = (i % 2 == 0 ? -1f : 1f) * (s.body.x * 0.5f + 0.10f);
            float z = (i < 2 ? 1f : -1f) * wz;
            Part(CarMesh.Arch(0.78f, 0.96f, 0.62f, 8f, 172f, 16), flare, v, "Fender" + i, new Vector3(x, 0f, z), Vector3.one);
        }

        // Gương + tay nắm cửa
        for (int side = -1; side <= 1; side += 2)
        {
            Tube(v, "MirrorArm" + side, new Vector3(side * 0.98f, 1.05f, 0.96f), new Vector3(side * 1.20f, 1.10f, 1.00f), 0.04f, black);
            Box(v, "Mirror" + side, new Vector3(0.06f, 0.18f, 0.24f), 0.02f, black, new Vector3(side * 1.23f, 1.10f, 1.00f));
            Box(v, "Handle" + side, new Vector3(0.05f, 0.05f, 0.24f), 0.01f, chrome, new Vector3(side * 1.02f, 0.86f, 0.55f));
        }

        // Ghế + tay đua trong ca-bin (thu nhỏ vừa khoang lái)
        var interior = new GameObject("Interior").transform;
        interior.SetParent(v, false);
        interior.localPosition = new Vector3(0f, 0.55f, 0.30f);
        interior.localScale = Vector3.one * 0.8f;
        BuildSeat(interior, "SeatDriver", new Vector3(-0.42f, 0f, 0f), new Color(0.14f, 0.14f, 0.15f));
        BuildSeat(interior, "SeatPass", new Vector3(0.42f, 0f, 0f), new Color(0.14f, 0.14f, 0.15f));
        BuildDriver(interior, new Vector3(-0.42f, 0.04f, 0.02f), 0f, driverColors[Mathf.Clamp(driver, 0, 2)]);
        Box(v, "Dash", new Vector3(1.70f, 0.16f, 0.28f), 0.04f, black, new Vector3(0f, 0.92f, 0.92f));
        SteeringWheel(v, new Vector3(-0.42f, 0.88f, 0.68f));

        // Lốp dự phòng trong thùng + cờ
        Part(CarMesh.Tire(24), MatHelper.MakeRubber(), v, "SpareTire", new Vector3(0f, 1.06f, -1.22f), new Vector3(0.72f, 0.24f, 0.72f), AxisZ);
        Disc(v, "SpareRim", 0.40f, 0.03f, chrome, new Vector3(0f, 1.06f, -1.35f), AxisZ);
        Tube(v, "Whip", new Vector3(-0.88f, 0.95f, -1.55f), new Vector3(-0.95f, 2.45f, -1.62f), 0.03f, black);
        Box(v, "Flag", new Vector3(0.02f, 0.36f, 0.55f), 0.005f, MatHelper.MakePaint(new Color(0.10f, 0.35f, 0.85f)), new Vector3(-0.95f, 2.30f, -1.35f));
    }
}
