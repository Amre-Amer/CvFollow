using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class cvFollow : MonoBehaviour {
    GameObject[] balls;
    Vector3[] targets;
    Vector3[] looks;
    GameObject head;
    GameObject body;
    GameObject tail;
    GameObject parent;
    GameObject pix;
    GameObject lin;
    Camera camCv;
    Color[] pixels;
    Texture2D texRt; 
    List<IntXYRect> blobs;
    List<int> joins;
    List<GameObject> bb;
    GameObject scr;
    GameObject trailLast;
    float distNear = 10;
    float radStage = 150;
    float speed = .25f;
    float speedBalls = .25f;
    float speedTurn = 5;
    float smooth = .95f;
    float nearClipPlaneDist = 1f;
    float fov = 60; //120;
    int pixelRes = 50; //512;
    float tolColor = .25f;
    int numBalls = 100;
    bool ynFoundPix;
    int iPix;
    int jPix;
    [Range(0, 100)]
    public float distCamera = 20;
    public bool ynBlob;
    public bool ynStep;
    public bool ynTrail;
    public bool ynHideBalls;
    bool ynHideBallsLast;
    float timeStep;
    public string layerName = "picturePlane";
    int frameCount;
    float colDistance;
    struct IntXY {
        public int x;
        public int y;
        public IntXY(int xNew, int yNew) {
            this.x = xNew;
            this.y = yNew;
        }
    }
    struct IntXYRect {
        public int xMin;
        public int yMin;
        public int xMax;
        public int yMax;
        public IntXYRect(int xMin0, int yMin0, int xMax0, int yMax0) {
            this.xMin = xMin0;
            this.yMin = yMin0;
            this.xMax = xMax0;
            this.yMax = yMax0;
        }
    }

	void Start () {
        initGos();
        initBalls();
        initCv();
	}
	
    void Update() {
        if (ynStep == true) {
            if (Time.realtimeSinceStartup - timeStep > 1)
            {
                timeStep = Time.realtimeSinceStartup;
                UpdateOne();
            }
        } else {
            UpdateOne();            
        }
    }

	void UpdateOne () {
        updateBalls();		
        updateCv();
        updateHead();
        updateFollow(head, body);
        updateFollow(body, tail);
        updateCamMain();
        updateTrail();
        frameCount++;
	}

    void updateTrail() {
        if (ynTrail == false) return;
        float dist = -1;
        if (trailLast) {
            dist = Vector3.Distance(trailLast.transform.position, tail.transform.position);
        }
        if (trailLast == null || dist > .3f)
        {
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.name = "trail";
            go.transform.parent = parent.transform;
            go.transform.eulerAngles = tail.transform.eulerAngles;
            go.transform.position = tail.transform.position;
            go.transform.localScale = new Vector3(.1f, .2f, .3f);
            Destroy(go, 20);
            trailLast = go;
        }
    }

    void updateCamMain() {
        Camera.main.transform.position = head.transform.position;
        Camera.main.transform.eulerAngles = head.transform.eulerAngles;
        Camera.main.transform.position += Vector3.forward * distCamera;
        Camera.main.transform.position += Vector3.up * distCamera/2;
        Camera.main.transform.position += Vector3.right * distCamera/2;
        Camera.main.transform.LookAt(head.transform.position);
    }

    void updateCv()
    {
        iPix = -1;
        jPix = -1;
        int bestN = -1;
        if (ynBlob == true)
        {
            bestN = getBestPixelBlob(Color.green);
        }
        else
        {
            bestN = getBestPixel(Color.green);
        }
        if (bestN >= 0)
        {
            iPix = convertN2I(bestN);
            jPix = convertN2J(bestN);
            updatePixel(iPix, jPix);
            //
            float scaZ = 50;
            adjustLine(lin, camCv.transform.position, pix.transform.position, scaZ);
            head.GetComponent<Renderer>().material.color = Color.white;
            //
            ynFoundPix = true;
        }
        else
        {
            head.GetComponent<Renderer>().material.color = Color.red;
            //ynFoundPix = false;
            scanHead();
        }
    }

    void scanHead()
    {
        iPix = (int)(pixelRes / 2 + pixelRes / 2 * Mathf.Cos(2 * frameCount * Mathf.Deg2Rad));
        Debug.Log(iPix);
        jPix = pixelRes / 2;
        updatePixel(iPix, jPix);
        adjustLine(lin, camCv.transform.position, pix.transform.position, 5);
    }

    void updateBalls() {
        for (int n = 0; n < numBalls; n++)
        {
            float distTarget = Vector3.Distance(balls[n].transform.position, targets[n]);
            float distHead = Vector3.Distance(balls[n].transform.position, head.transform.position);
            if (distTarget < distNear)
            {
                targets[n] = head.transform.position + Random.insideUnitSphere * distNear * 2;
            }
            if (distHead > radStage)
            {
                targets[n] = head.transform.position + Random.insideUnitSphere * radStage;
            }
            looks[n] = smooth * looks[n] + (1 - smooth) * targets[n];
            balls[n].transform.LookAt(looks[n]);
            balls[n].transform.position += balls[n].transform.forward * speedBalls;                
            //
            updateHideBall(n);
        }
        ynHideBallsLast = ynHideBalls;
    }

    void updateHideBall(int n)
    {
        if (ynHideBalls != ynHideBallsLast) {
            if (ynHideBalls == true) {
                balls[n].GetComponent<Renderer>().material.color = Color.red;
            } else {
                balls[n].GetComponent<Renderer>().material.color = Color.green;
            }
        }
    }

   void updateHead() {
        if (ynFoundPix == true) {
            float y = jPix / (float)pixelRes;
            y = -1 * 2 * (y - .5f);
            float x = iPix / (float)pixelRes;
            x = 2 * (x - .5f);
//            head.transform.Rotate(y * speedTurn, x * speedTurn, -1 * head.transform.eulerAngles.z);
            head.transform.Rotate(y * speedTurn, x * speedTurn, 0);
            head.transform.position += head.transform.forward * speed;                
        }
        //
    }

    void updateFollow(GameObject leader, GameObject follower) {
        Vector3 posLeaderEnd = leader.transform.position + leader.transform.forward * -1 * leader.transform.localScale.z / 2;
        follower.transform.LookAt(posLeaderEnd);
        float dist = Vector3.Distance(follower.transform.position, posLeaderEnd);
        float distMin = follower.transform.localScale.z / 2;
        follower.transform.position += follower.transform.forward * (dist - distMin);
    }

    int getBestPixel(Color col)
    {
        RenderTexture.active = camCv.targetTexture;
        texRt = new Texture2D(pixelRes, pixelRes, TextureFormat.RGBA32, false);
        texRt.ReadPixels(new Rect(0, 0, pixelRes, pixelRes), 0, 0);
        texRt.Apply();
        RenderTexture.active = null;
        pixels = texRt.GetPixels(0, 0, pixelRes, pixelRes);
        int bestN = -1;
        float bestDist = 1000;
        for (int n = 0; n < pixels.Length; n++)
        {
            Color c = pixels[n];
            float distColor = Vector3.Distance(new Vector3(c.r, c.g, c.b), new Vector3(col.r, col.g, col.b));
            if (distColor < bestDist || n == 0)
            {
                bestN = n;
                bestDist = distColor;
            }
        }
//        Debug.Log(bestDist);
        if (bestDist > tolColor)
        {
            bestN = -1;
            colDistance = bestDist;
//            Debug.Log("> " + tolColor + " " + colDistance);
        }
        return bestN;
    }








    int getBestPixelBlob(Color col)
    {
        int bestN = -1;
        blobs = new List<IntXYRect>();
        //joins = new List<int>();
        if (bb != null)
        {
            for (int b = 0; b < bb.Count; b++) {
                GameObject b0 = bb[b];
                Destroy(b0);
            }
        }
        bb = new List<GameObject>();
        //
        int cnt = 0;
        int cntAdd = 0;
        int cntJoin = 0;
        int cntJoinTot = 0;
        int cntAddTot = 0;
        RenderTexture.active = camCv.targetTexture;
        texRt = new Texture2D(pixelRes, pixelRes, TextureFormat.RGBA32, false);
        texRt.ReadPixels(new Rect(0, 0, pixelRes, pixelRes), 0, 0);
        texRt.Apply();
        RenderTexture.active = null;
        pixels = texRt.GetPixels(0, 0, pixelRes, pixelRes);
        for (int n = 0; n < pixels.Length; n++)
        {
            Color c = pixels[n];
            float distColor = Vector3.Distance(new Vector3(c.r, c.g, c.b), new Vector3(col.r, col.g, col.b));
            if (distColor < tolColor)
            {
                cnt++;
                joins = new List<int>();
                cntAdd = 0;
                cntJoin = 0;
                IntXY p = convertN2IntXY(n);
                for (int b = 0; b < blobs.Count; b++)
                {
                    IntXYRect blob = blobs[b];
                    if (p.x == blob.xMin - 1) {
                        blob.xMin--;
                        blobs[b] = blob;
                        joins.Add(b);
                        cntJoin++;
                    }
                    if (p.x == blob.xMax + 1) {
                        blob.xMax++;
                        blobs[b] = blob;
                        joins.Add(b);
                        cntJoin++;
                    }
                    if (p.y == blob.yMin - 1)
                    {
                        blob.yMin--;
                        blobs[b] = blob;
                        joins.Add(b);
                        cntJoin++;
                    }
                    if (p.y == blob.yMax + 1)
                    {
                        blob.yMax++;
                        blobs[b] = blob;
                        joins.Add(b);
                        cntJoin++;
                    }
                }
                if (cntJoin == 0) {
                    blobs.Add(new IntXYRect(p.x, p.y, p.x, p.y));
                    cntAdd++;
                }
                if (cntJoin > 1) {
                //    IntXYRect join0 = blobs[joins[0]];
                //    for (int j = 1; j < joins.Count; j++) {
                //        if (blobs[joins[j]].xMin < join0.xMin) join0.xMin = blobs[joins[j]].xMin;
                //        if (blobs[joins[j]].xMax > join0.xMax) join0.xMax = blobs[joins[j]].xMax;
                //        if (blobs[joins[j]].yMin < join0.yMin) join0.yMin = blobs[joins[j]].yMin;
                //        if (blobs[joins[j]].yMax > join0.yMax) join0.yMax = blobs[joins[j]].yMax;
                //        blobs.RemoveAt(joins[j]);
                //    }
                }
                cntAddTot += cntAdd;
                cntJoinTot += cntJoin;
            }
        }
        int bBest = -1;
        int bestArea = -1;
        IntXYRect bestBlob = new IntXYRect(0, 0, 0, 0);
        for (int b = 0; b < blobs.Count; b++) {
            int area = (blobs[b].xMax - blobs[b].xMin) * (blobs[b].yMax - blobs[b].yMin);            
            if (b == 0 || area > bestArea) {
                bestArea = area;
                bestBlob = blobs[b];
                bBest = b;
            }
        }
        //Debug.Log(bestN + " " + blobs.Count + " " + bestBlob.xMin + " " + bestBlob.xMax);
        //Debug.Log("pix dist: " + dist);
        for (int b = 0; b < blobs.Count; b++)
        {
            IntXYRect blob = blobs[b];
            int ib = (blob.xMin + blob.xMax) / 2;
            int jb = (blob.yMin + blob.yMax) / 2;
            bestN = convertIJ2N(ib, jb);
            Vector3 pos = camCv.ScreenToWorldPoint(new Vector3(ib, jb, camCv.nearClipPlane));
            Vector3 posR = camCv.ScreenToWorldPoint(new Vector3(ib + 1, jb, camCv.nearClipPlane));
            float dist = Vector3.Distance(pos, posR);
            GameObject go = GameObject.CreatePrimitive(PrimitiveType.Cube);
            go.transform.position = pos;
            go.transform.eulerAngles = camCv.transform.eulerAngles;
            float scaX = (bestBlob.xMax - bestBlob.xMin + 1) * dist;
            float scaY = (bestBlob.yMax - bestBlob.yMin + 1) * dist;
            go.transform.localScale = new Vector3(scaX, scaY, 2);
            if (b == bBest) {
                go.GetComponent<Renderer>().material.color = Color.red;
            } else {
                go.GetComponent<Renderer>().material.color = Color.white;
            }
            hideLayer(go);
            bb.Add(go);
        }
        if (ynStep == true)
        {
            Debug.Log("found:" + cnt + " add:" + cntAddTot + " joins:" + cntJoinTot + " blobs:" + blobs.Count + " best:" + (bestBlob.xMax - bestBlob.xMin) + " x " + (bestBlob.yMax - bestBlob.yMin));
        }
        int iBest = (bestBlob.xMin + bestBlob.xMax) / 2;
        int jBest = (bestBlob.yMin + bestBlob.yMax) / 2;
        bestN = convertIJ2N(iBest, jBest);
        return bestN;
    }

    IntXY convertN2IntXY(int n) {
        return new IntXY(convertN2I(n), convertN2J(n));
    }

    int convertIJ2N(int i, int j) {
        return j * pixelRes + i;        
    }

    int convertN2I(int n) {
        return n % pixelRes;
    }

    int convertN2J(int n)
    {
        return n / pixelRes;
    }

    void adjustLine(GameObject go, Vector3 p1, Vector3 p2, float scaZ)
    {
        go.transform.position = (p1 + p2) / 2;
        float dist = Vector3.Distance(p1, p2);
        go.transform.localScale = new Vector3(.1f, .1f, scaZ);
        go.transform.LookAt(p2);
        go.transform.position += go.transform.forward * scaZ / 2;
    }

    void updatePixel(int i, int j)
    {
        pix.transform.position = camCv.ScreenToWorldPoint(new Vector3(i, j, camCv.nearClipPlane + .5f));
    }

    void initBalls() {
        balls = new GameObject[numBalls];
        targets = new Vector3[numBalls];
        looks = new Vector3[numBalls];
        for (int n = 0; n < numBalls; n++) {
            //
            balls[n] = initGo("ball" + n, 1, 1, Color.green, PrimitiveType.Sphere);
            //balls[n].GetComponent<Renderer>().material = new Material(Shader.Find("Unlit/Color"));
            balls[n].GetComponent<Renderer>().material.color = Color.green;
        } 
    }

    void initGos() {
        parent = new GameObject("parent");
        parent.transform.parent = transform;
        head = initGo("head", 3, .5f, Color.white, PrimitiveType.Sphere);
        body = initGo("body", 2, 2, Color.yellow, PrimitiveType.Sphere);
        tail = initGo("tail", 1, 2, Color.red, PrimitiveType.Sphere);
        pix = initGo("pix", .5f, .75f, Color.yellow, PrimitiveType.Sphere);
        lin = initGo("lin", 1, 1, Color.black, PrimitiveType.Cube);
        //
        hideLayer(head);
        hideLayer(pix);
        hideLayer(lin);
        //
        hideLayer(body);
        hideLayer(tail);
        //
    }

    void initCv() {
        camCv = head.AddComponent<Camera>();
        camCv.targetDisplay = 0;
        camCv.cullingMask &= ~(1 << LayerMask.NameToLayer(layerName));
        camCv.clearFlags = CameraClearFlags.SolidColor;
        camCv.nearClipPlane = nearClipPlaneDist;
        camCv.fieldOfView = fov;
        camCv.targetTexture = new RenderTexture(pixelRes, pixelRes, 24, RenderTextureFormat.ARGB32);
        //
        scr = GameObject.CreatePrimitive(PrimitiveType.Cube);
        scr.name = "scr";
        scr.GetComponent<Renderer>().material.color = Color.blue;
        Vector3 ll = camCv.ScreenToWorldPoint(new Vector3(0, 0, camCv.nearClipPlane));
        Vector3 lr = camCv.ScreenToWorldPoint(new Vector3(pixelRes, 0, camCv.nearClipPlane));
        Vector3 ul = camCv.ScreenToWorldPoint(new Vector3(0, pixelRes, camCv.nearClipPlane));
        scr.transform.position = camCv.transform.position + camCv.transform.forward * camCv.nearClipPlane;
        scr.transform.eulerAngles = camCv.transform.eulerAngles;
        scr.transform.localScale = new Vector3(Vector3.Distance(ll, lr), Vector3.Distance(ll, ul), .1f);
        scr.transform.parent = camCv.transform;
        hideLayer(scr);
    }

    GameObject initGo(string txt, float dia, float stretch, Color col, PrimitiveType pType) {
        GameObject go = GameObject.CreatePrimitive(pType);
        go.name = txt;
        go.transform.parent = parent.transform;
        go.GetComponent<Renderer>().material.color = col;
        go.transform.localScale = new Vector3(dia, dia, dia * stretch);
        return go;
    }

    void hideLayer(GameObject go)
    {
        go.layer = LayerMask.NameToLayer(layerName);
    }
}
