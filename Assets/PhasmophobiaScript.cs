using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using KModkit;

public class PhasmophobiaScript : MonoBehaviour{

    //globals
    public KMBombInfo bomb;
    public KMAudio Audio;

    //logging
    static int moduleIdCounter = 1;
    int moduleId;
    string remove = " (Instance)";
    bool moduleSolved = false; 

    //Selectables
    public KMSelectable uNav;
    public KMSelectable dNav;
    public KMSelectable uSlot;
    public KMSelectable dSlot;
    public KMSelectable[] tools;
    public KMSelectable lScroll;
    public KMSelectable rScroll;
    public KMSelectable submit;
    public KMSelectable closet;

    //Location
    public TextMesh locName;
    int locIndex = 0;
    readonly string[] locOptions = { "TangleWood Dr.", "Edgefield Rd.", "Ridgeview Ct.", "Grafton Farmhouse", "Willow St.", "Bleasdale Farmhouse" };

    //Rooms
    readonly string[] roomOptions = { "Van", "", "Living Room", "Dining Room", "Kitchen", "Master Bedroom", "Master Bathroom", "Guest Bedroom", "Guest Bathroom", "Kid's Bedroom", "Garage", "" };
    int roomIndex = -1; //No closet - shouldn't update when closet is selected
    int _roomIndex = 1; //With closet - should update when closet is selected
    public Renderer roomPic;
    public Material[] roomMats;
    public Material vanMat;
    public Material[] closMats;
    public Material[] locMats;
    string[] _roomOptions;
    bool inCloset = false;

    //Favorite Room
    int favRoomIndex;
    int _correctCol; 
    bool inFavRoom = false;
    bool flickering = false; 
    int favToolIndex; 
    int[] favToolOptions;

    //Ghost
    public TextMesh ghoName;
    readonly string[] ghoOptions = { "Banshee", "Demon", "Deogen", "Goryo", "Hantu", "Jinn", "Mare", "Moroi", "Myling", "Obake", "Oni", "Onryo", "Phantom", "Poltergeist", "Raiju", "Revenant", "Shade", "Spirit", "Thaye", "Mimic", "Twins", "Wraith", "Yokai", "Yurei" };
    int ghoIndex;
    int trueGhoIndex;

    //Sanity
    int sanity = 100; 
    public TextMesh sanityMeter; 

    //Hunting
    bool hunting = false; 
    bool buffer = false; 
    int huntNumber = 0; 

    //Slots
    public Renderer uSlotPic;
    public Renderer uSlotEdge;
    public Renderer dSlotPic;
    public Renderer dSlotEdge;
    string[] _toolMats;
    bool uEmpty = true;
    bool dEmpty = true;
    bool uLit = false;
    bool dLit = false;

    //Tools
    public Material[] toolMats;
    public Renderer[] toolPics;
    List<string> intermediate = new List<string>();

    //Objects
    public GameObject flashLight;
    public GameObject equipment;
    public GameObject scroll;
    public GameObject _closet;
	public GameObject notepad;
	public GameObject[] camOrbs;
	public GameObject dots;
    public GameObject flake; 
    public GameObject uv; 
    Color32 flashLightColor; 

    //Evidence
    int correctCol;//0 Book, 1 Cam, 2 Dots, 3 EMF, 4 SB, 5 Thermo, 6 UV
	readonly string[] toolNames = { "Notebook", "Camera", "D.O.T.S", "EMF", "Spirit Box", "Thermometer", "Black Light" };
	
	public TextMesh bookWords;
	readonly string[] wordOptions = { "Run", "Afraid", "Go Away", "No Escape", "Boom", "Strike", "Behind You" };
	string correctWord;
    float randFloat;
    string[] boxSounds = { "sbhere", "sbadult", "sbkill", "sbbaby", "sbbehind", "sbdad", "sbdaughter", "sbfar" };
	int[] orbOptions = { 2, 3, 1, 4 };
	int correctOrbs;
	float xFloat;
	float zFloat; 
    Color32 camColorG = new Color32 (0x58, 0xFF, 0x57, 0xFF);
    Color32 camColor = new Color32(0xFF, 0xFF, 0xFF, 0xFF);
	public Material[] dotMats;
    int[] beepNumber = { 3, 1, 2, 4 };
    public Material[] flakeMats;
    Color32 coldColor = new Color32(0xDF, 0xFD, 0xFF, 0xFF);
    public Material[] uvMats;
    Color32 uvLightColor = new Color32(0x81, 0x4F, 0xFF, 0xFF);

	bool cooldown;
	bool[] correctEvidence = new bool[7];


    void Awake(){ //base light = FFF892FF, intense = 50, UV = 814FFFFF, intense = 30
        moduleId = moduleIdCounter++;

        uNav.OnInteract += delegate () { UNavPress(); return false; };
        dNav.OnInteract += delegate () { DNavPress(); return false; };

        uSlot.OnInteract += delegate () { USlotPress(); return false; };
        dSlot.OnInteract += delegate () { DSlotPress(); return false; };

        lScroll.OnInteract += delegate () { LScrollPress(); return false; };
        rScroll.OnInteract += delegate () { RScrollPress(); return false; };

        submit.OnInteract += delegate () { SubmitPress(); return false; };

        closet.OnInteract += delegate () { ClosetPress(); return false; };

        foreach (KMSelectable tool in tools){
            KMSelectable pressedTool = tool;
            tool.OnInteract += delegate () { ToolPress(pressedTool); return false; };
        }
    }

    void Start(){
        Disable();
        PickLocation();
        PickGhost();
        RandomizeCloset();
        PickFavoriteRoom();
        FindColumn();
        StartCoroutine(SanityOverTime());
    }

    void Disable(){
        equipment.SetActive(false);
        scroll.SetActive(false);
        _closet.SetActive(false);
		notepad.SetActive(false);
        foreach (GameObject orb in camOrbs){orb.SetActive(false);}
		dots.SetActive(false);
        flake.SetActive(false);
        uv.SetActive(false);
        ghoName.text = ghoOptions[ghoIndex];
        uSlotPic.material = toolMats[7];
        dSlotPic.material = toolMats[7];
        flashLightColor = new Color32(0xFF, 0xF8, 0x92, 0xFF);
        flashLight.GetComponent<Light>().color = flashLightColor;
        flashLight.GetComponent<Light>().intensity = 50f;
        flashLight.GetComponent<Light>().spotAngle = 27.2f;
        bookWords.text = "";

        for (int j = 0; j < toolMats.Length; j++){
            intermediate.Add(toolMats[j].ToString());
        }
        _toolMats = intermediate.ToArray();
    }

    void LScrollPress(){
        lScroll.AddInteractionPunch(0.3f);
        if (ghoIndex == 0){ghoIndex = ghoOptions.Length - 1;}
        else{ghoIndex--;}
        ghoName.text = ghoOptions[ghoIndex];
    }

    void RScrollPress(){
        rScroll.AddInteractionPunch(0.3f);
        if (ghoIndex == ghoOptions.Length - 1){ghoIndex = 0;}
        else{ghoIndex++;}
        ghoName.text = ghoOptions[ghoIndex];
    }

    void USlotPress(){
        uSlot.AddInteractionPunch(0.3f);
        if (moduleSolved == true){return;}
        if (_roomIndex == 0 && uEmpty == false){
            switch (Array.IndexOf(_toolMats, uSlotPic.material.ToString().Replace(remove, ""))){
                case 0: toolPics[0].material = toolMats[0]; break;
                case 1: toolPics[1].material = toolMats[1]; break;
                case 2: toolPics[2].material = toolMats[2]; break;
                case 3: toolPics[3].material = toolMats[3]; break;
                case 4: toolPics[4].material = toolMats[4]; break;
                case 5: toolPics[5].material = toolMats[5]; break;
                case 6: toolPics[6].material = toolMats[6]; break;
                default: break;
            }
            uSlotPic.material = toolMats[7];
            uEmpty = true;
            if (uLit == true){
                LightChange(0);
            }
        }

		else if (uLit == true && cooldown == false){LightChange(0);}

        else if (_roomIndex > 1 && uEmpty == false && dLit == false && cooldown == false){ //Must be in the house, have tool in upper slot, lower slot cant be active, and cant be on cooldown
            LightChange(0);
            if (hunting == true){return;}
            int sanDec = UnityEngine.Random.Range(2,5); 
            while (sanity-sanDec < 0){
                sanDec = sanity; 
            }
            sanity -= sanDec; 
            sanityMeter.text = sanity.ToString() + "%";
            if (inFavRoom == true && favToolIndex == Array.IndexOf(_toolMats, uSlotPic.material.ToString().Replace(remove, ""))){StartCoroutine(FavRoomFlicker());}
            switch (Array.IndexOf(_toolMats, uSlotPic.material.ToString().Replace(remove, ""))){
                case 0: BookEv(); break;
                case 1: CamEv(); break;
                case 2: DotEv(); break;
                case 3: EmfEv(); break;
                case 4: SpiritEv(); break;
                case 5: ThermEv(); break;
                case 6: UvEv(); break;
                default: break;
            }
        }
    }

    void DSlotPress(){
        dSlot.AddInteractionPunch(0.3f);
        if (moduleSolved == true){return;}
        if (_roomIndex == 0 && dEmpty == false){
            switch (Array.IndexOf(_toolMats, dSlotPic.material.ToString().Replace(remove, ""))){
                case 0: toolPics[0].material = toolMats[0]; break;
                case 1: toolPics[1].material = toolMats[1]; break;
                case 2: toolPics[2].material = toolMats[2]; break;
                case 3: toolPics[3].material = toolMats[3]; break;
                case 4: toolPics[4].material = toolMats[4]; break;
                case 5: toolPics[5].material = toolMats[5]; break;
                case 6: toolPics[6].material = toolMats[6]; break;
                default: break;
            }
            dSlotPic.material = toolMats[7];
            dEmpty = true;
            if (dLit == true){
                LightChange(1);
            }
        }
		else if (dLit == true && cooldown == false){LightChange(1);}

        else if (_roomIndex > 1 && dEmpty == false && uLit == false && cooldown == false){ //Must be in the house, have tool in lower slot, upper slot cant be active, and cant be on cooldown
            LightChange(1);
            if (hunting == true){return;}
            int sanDec = UnityEngine.Random.Range(2,5); 
            while (sanity-sanDec < 0){
                sanDec = sanity; 
            }
            sanity -= sanDec; 
            sanityMeter.text = sanity.ToString() + "%";
            if (inFavRoom == true && favToolIndex == Array.IndexOf(_toolMats, dSlotPic.material.ToString().Replace(remove, ""))){
                StartCoroutine(FavRoomFlicker());}
            switch (Array.IndexOf(_toolMats, dSlotPic.material.ToString().Replace(remove, ""))){
                case 0: BookEv(); break;
                case 1: CamEv(); break;
                case 2: DotEv(); break;
                case 3: EmfEv(); break;
                case 4: SpiritEv(); break;
                case 5: ThermEv(); break;
                case 6: UvEv(); break;
                default: break;
            }
        }
    }

    void ToolPress(KMSelectable tool){
        tool.AddInteractionPunch(0.3f);
        if (moduleSolved == true){return;}
        if (uEmpty == true){
            int x = (Array.IndexOf(_toolMats, tool.GetComponent<MeshRenderer>().material.ToString().Replace(remove, "")));
            toolPics[x].material = toolMats[7];
            uSlotPic.material = toolMats[x];
            uEmpty = false;
        }
        else if (dEmpty == true){
            int x = (Array.IndexOf(_toolMats, tool.GetComponent<MeshRenderer>().material.ToString().Replace(remove, "")));
            toolPics[x].material = toolMats[7];
            dSlotPic.material = toolMats[x];
            dEmpty = false;
        }
    }

    void UNavPress(){
        uNav.AddInteractionPunch(0.3f);
        if(_roomIndex == _roomOptions.Length - 2 || moduleSolved == true){
            return;
        }
        else{
            if (_roomOptions[_roomIndex + 1] != "Closet"){
                roomIndex++;//updates when next isnt closet
            }
            _roomIndex++;//always updates 
            ChangeLoc();
            dNav.gameObject.SetActive(true);
        }
        if (_roomIndex > 0){
            flashLight.SetActive(true);
        }
    }

    void DNavPress(){
        dNav.AddInteractionPunch(0.3f);
        if (_roomIndex == 0 || moduleSolved == true){
            return;
        }
        else{
            if (_roomOptions[_roomIndex] != "Closet"){
                roomIndex--;//updates when next isnt closet 
            }
            _roomIndex--;//always updates
            ChangeLoc();
            uNav.gameObject.SetActive(true);
        }
        if (_roomIndex < 1){
            flashLight.SetActive(false);
        }
    }

    void SubmitPress(){
        submit.AddInteractionPunch(0.8f);
        if (moduleSolved == true){return;}
        if (ghoIndex == trueGhoIndex){
            moduleSolved = true; 
            StopAllCoroutines();
            StartCoroutine(Solved());
            Debug.LogFormat("Phasmophobia #{0}: Correct submission. Module solved!", moduleId);
        }
        else{
            StopAllCoroutines(); 
            StartCoroutine(Strike()); 
            Debug.LogFormat("Phasmophobia #{0}: Incorrect submission.", moduleId);
        }
    }

    IEnumerator Solved(){
        Audio.PlaySoundAtTransform("dead2", transform);
        int it = 0; 
        int sanDec = 1; 
        float time; 
        while (it < 100){ 
            time = Math.Max(0.01f, 0.5f-0.035f*it); 
            yield return new WaitForSeconds(time);
            sanity -= sanDec;
            sanityMeter.text = sanity.ToString() + "%";
            it++;
        } 
        yield return null; 
        Audio.PlaySoundAtTransform("victory", transform);
        roomPic.material = locMats[locIndex]; 
        uSlot.gameObject.SetActive(false);
        dSlot.gameObject.SetActive(false);
        uNav.gameObject.SetActive(false);
        dNav.gameObject.SetActive(false);
        lScroll.gameObject.SetActive(false);
        rScroll.gameObject.SetActive(false);
        sanityMeter.text = "sane";
        equipment.gameObject.SetActive(false); 
        submit.gameObject.SetActive(false); 
        locName.text = "Ghost Identified!";
        GetComponent<KMBombModule>().HandlePass();
        StopAllCoroutines();
        yield break; 
    }

    IEnumerator Strike(){
        StopCoroutine(SanityOverTime());
        moduleSolved = true; //not actually, just used to disable button presses
        Audio.PlaySoundAtTransform("dead", transform);
        int it = 0; 
        int sanDec = 1; 
        float time; 
        while (it < 100){ 
            time = Math.Max(0.01f, 0.5f-0.035f*it); 
            yield return new WaitForSeconds(time);
            sanity -= sanDec;
            sanityMeter.text = sanity.ToString() + "%";
            it++;
        } 
        yield return null; 
        GetComponent<KMBombModule>().HandleStrike(); 
        roomIndex = -1; 
        _roomIndex = 1; 
        ChangeLoc(); 
        sanity = 100; 
        sanityMeter.text = sanity.ToString() + "%";
        buffer = false; 
        cooldown = false; 
        flickering = false; 
        inCloset = false;
        hunting = false; 
        uNav.gameObject.SetActive(true); 
        dNav.gameObject.SetActive(true); 
        StartCoroutine(SanityOverTime()); 
        moduleSolved = false; 
        yield break; 
    }

    void ClosetPress(){
        closet.AddInteractionPunch(0.5f);
        if (inCloset == false){
            roomPic.material = closMats[1];
            uNav.gameObject.SetActive(false);
            dNav.gameObject.SetActive(false);
            locName.text = "Closet (Inside)";
        }
        else{
            roomPic.material = closMats[0];
            uNav.gameObject.SetActive(true);
            dNav.gameObject.SetActive(true);
            locName.text = "Closet (Outside)";
        }
        inCloset = !inCloset;
    }

    void PickLocation(){
        locIndex = UnityEngine.Random.Range(0, locOptions.Length);
        locName.text = locOptions[locIndex];
        roomPic.material = locMats[locIndex];
        Debug.LogFormat("Phasmophobia #{0}: Location is {1}", moduleId, locOptions[locIndex]);
    }

    void PickGhost(){
        trueGhoIndex = UnityEngine.Random.Range(0, ghoOptions.Length);
        Debug.LogFormat("Phasmophobia #{0}: Correct ghost type is {1}", moduleId, ghoOptions[trueGhoIndex]);
		switch (trueGhoIndex){ //0 Book, 1 Cam, 2 Dots, 3 EMF, 4 SB, 5 Thermo, 6 UV
			case 0: correctEvidence[1] = true; correctEvidence[2] = true; correctEvidence[6] = true;break;//Banshee
			case 1: correctEvidence[0] = true; correctEvidence[5] = true; correctEvidence[6] = true;break;//Demon
			case 2: correctEvidence[0] = true; correctEvidence[2] = true; correctEvidence[4] = true;break;//Deogen
			case 3: correctEvidence[2] = true; correctEvidence[3] = true; correctEvidence[6] = true;break;//goryo
			case 4: correctEvidence[1] = true; correctEvidence[5] = true; correctEvidence[6] = true;break;//hantu
			case 5: correctEvidence[3] = true; correctEvidence[5] = true; correctEvidence[6] = true;break;//jinn
			case 6: correctEvidence[0] = true; correctEvidence[1] = true; correctEvidence[4] = true;break;//mare
			case 7: correctEvidence[0] = true; correctEvidence[4] = true; correctEvidence[5] = true;break;//moroi
			case 8: correctEvidence[0] = true; correctEvidence[3] = true; correctEvidence[6] = true;break;//myling
			case 9: correctEvidence[1] = true; correctEvidence[3] = true; correctEvidence[6] = true;break;//obake
			case 10: correctEvidence[2] = true; correctEvidence[3] = true; correctEvidence[5] = true;break;//oni
			case 11: correctEvidence[1] = true; correctEvidence[4] = true; correctEvidence[5] = true;break;//onryo
			case 12: correctEvidence[2] = true; correctEvidence[4] = true; correctEvidence[6] = true;break;//phantom
			case 13: correctEvidence[0] = true; correctEvidence[4] = true; correctEvidence[6] = true;break;//poltergeist
			case 14: correctEvidence[1] = true; correctEvidence[2] = true; correctEvidence[3] = true;break;//raiju
			case 15: correctEvidence[0] = true; correctEvidence[1] = true; correctEvidence[5] = true;break;//revenant
			case 16: correctEvidence[0] = true; correctEvidence[3] = true; correctEvidence[5] = true;break;//shade
			case 17: correctEvidence[0] = true; correctEvidence[3] = true; correctEvidence[4] = true;break;//spirit
			case 18: correctEvidence[0] = true; correctEvidence[1] = true; correctEvidence[2] = true;break;//thaye
			case 19: correctEvidence[4] = true; correctEvidence[5] = true; correctEvidence[6] = true;break;//mimic
			case 20: correctEvidence[3] = true; correctEvidence[4] = true; correctEvidence[5] = true;break;//twins
			case 21: correctEvidence[2] = true; correctEvidence[3] = true; correctEvidence[4] = true;break;//wraith
			case 22: correctEvidence[1] = true; correctEvidence[2] = true; correctEvidence[4] = true;break;//yokai
			case 23: correctEvidence[1] = true; correctEvidence[2] = true; correctEvidence[5] = true;break;//yurei
		}
		List<string> _correctEvidence = new List<string>();
		for ( int i = 0; i < correctEvidence.Length; i++){
			if (correctEvidence[i] == true)
				_correctEvidence.Add(toolNames[i]);
		}
		Debug.LogFormat("Phasmophobia #{0}: Correct evidence is {1}, {2}, {3}", moduleId, _correctEvidence[0], _correctEvidence[1], _correctEvidence[2]);
    }

    void ChangeLoc(){
        if (_roomIndex == 0){
            locName.text = _roomOptions[_roomIndex];
            roomPic.material = vanMat;
            dNav.gameObject.SetActive(false);
            equipment.SetActive(true);
            scroll.SetActive(true);
			inFavRoom = false;
        }
        else if (_roomIndex == 1){
            locName.text = locOptions[locIndex];
            roomPic.material = locMats[locIndex];
            equipment.SetActive(false);
            scroll.SetActive(false);
            _closet.SetActive(false);
			inFavRoom = false;
        }
        else if (_roomOptions[_roomIndex] == "Closet"){
            locName.text = "Closet (Outside)";
            roomPic.material = closMats[0];
            _closet.SetActive(true);
			inFavRoom = false;
        }
        else{
            locName.text = _roomOptions[_roomIndex];
            roomPic.material = roomMats[roomIndex];
            _closet.SetActive(false);
            if (_roomOptions[_roomIndex] == "Garage"){
                uNav.gameObject.SetActive(false);
            }
            else if(hunting==true && roomIndex == 0){
                dNav.gameObject.SetActive(false);
                Audio.PlaySoundAtTransform("door", transform);
            }
            if (roomIndex == favRoomIndex){
                inFavRoom = true;
            }
			else{
				inFavRoom = false;
			}
        }
    }

    void RandomizeCloset(){
        string closet = "Closet";
        int insertIndex = UnityEngine.Random.Range(3, roomOptions.Length - 1);
        List<string> interim = roomOptions.ToList();
        interim.Insert(insertIndex, closet);
        _roomOptions = interim.ToArray();
        Debug.LogFormat("Phasmophobia #{0}: The closet is located between the {1} and the {2}.", moduleId, _roomOptions[insertIndex - 1], _roomOptions[insertIndex + 1]);
    }

    void PickFavoriteRoom(){
        favRoomIndex = UnityEngine.Random.Range(0, roomOptions.Length - 3);
        Debug.LogFormat("Phasmophobia #{0}: The favorite room is the {1}", moduleId, roomOptions[favRoomIndex + 2]);
    }

    void LightChange(int x){
        Audio.PlaySoundAtTransform("onoff", transform);
        switch (x){
            case 0:
                uLit = !uLit;
                if (uLit == false)
                {
                    uSlotEdge.material = toolMats[9];
                }
                else
                {
                    uSlotEdge.material = toolMats[8];
                }
                break;
            case 1:
                dLit = !dLit;
                if (dLit == false)
                {
                    dSlotEdge.material = toolMats[9];
                }
                else
                {
                    dSlotEdge.material = toolMats[8];
                }
                break;
            default: break;
        }
    }

	void RandomFloat(float min, float max){
			System.Random random = new System.Random();
			randFloat = (float)(min + (random.NextDouble()*(max-min)));
	}

    void BookEv(){
		if (_roomOptions[_roomIndex] == "Closet"){return;}
		else{correctWord = wordOptions[correctCol];	StartCoroutine(WriteWords());}
    }

	IEnumerator WriteWords(){

		int i = 0;
		string writtenWord = ""; 
		uNav.gameObject.SetActive(false);
		dNav.gameObject.SetActive(false);
		cooldown = true;
		yield return new WaitForSeconds(0.5f);
        while (flickering ==true){yield return null;}
        flashLight.SetActive(false);
        notepad.SetActive(true);
        Audio.PlaySoundAtTransform("writing", transform);
        int dud = UnityEngine.Random.Range(0,100);
		if (inFavRoom == true && correctEvidence[0] == true && dud<=60){
			while (i <= correctWord.Length-1) {
				writtenWord += correctWord[i];
				RandomFloat(0.1f, 0.4f);
			yield return new WaitForSeconds(randFloat);
			bookWords.text = writtenWord;
			i++;
			}
		}
		
		else{
			int randWordIndex = UnityEngine.Random.Range(0, wordOptions.Length-1);
			while (randWordIndex == correctCol){
				randWordIndex = UnityEngine.Random.Range(0, wordOptions.Length-1);
			}
			string randWord = wordOptions[randWordIndex];
			while (i <= randWord.Length-1) {
				writtenWord += randWord[i];
				RandomFloat(0.1f, 0.4f);
			yield return new WaitForSeconds(randFloat);
			bookWords.text = writtenWord;
			i++;
			}
		}
		
		yield return new WaitForSeconds(1.0f);
		bookWords.text = "";
		cooldown = false;
		uNav.gameObject.SetActive(true);
		dNav.gameObject.SetActive(true);
		flashLight.SetActive(true);
		notepad.SetActive(false);
		yield break;
	}

    void CamEv(){
		if (_roomOptions[_roomIndex] == "Closet"){return;}
		else{correctOrbs = orbOptions[correctCol];StartCoroutine(GhostOrbs());}
    }

	IEnumerator GhostOrbs(){ //x[-0.065, .024] z[-0.048,0.041]
		int it = 0;
        int dud = UnityEngine.Random.Range(0,100);
        uNav.gameObject.SetActive(false);
		dNav.gameObject.SetActive(false);
        cooldown = true; 
		yield return new WaitForSeconds(0.8f);
        while (flickering ==true){yield return null;}
        flashLight.SetActive(false);
        roomPic.material.color = camColorG; 
        Audio.PlaySoundAtTransform("cambeep", transform);

		if (inFavRoom == true && correctEvidence[1] == true && dud<=60){
			yield return new WaitForSeconds(1.0f);
			for (int i = 0; i < correctOrbs;i++) { 
				camOrbs[i].SetActive(true);
				camOrbs[i].transform.localPosition = new Vector3(UnityEngine.Random.Range(-0.055f, 0.014f), camOrbs[i].transform.localPosition.y, UnityEngine.Random.Range(-0.038f, 0.031f));
			}
		}
		else{
			int randOrbIndex = UnityEngine.Random.Range(1, 5);
			while (randOrbIndex == correctOrbs){
				randOrbIndex = UnityEngine.Random.Range(1, 5);
			}
			for (int i = 0; i < randOrbIndex;i++) { 
				camOrbs[i].SetActive(true);
				camOrbs[i].transform.localPosition = new Vector3(UnityEngine.Random.Range(-0.065f, 0.024f), camOrbs[i].transform.localPosition.y, UnityEngine.Random.Range(-0.048f, 0.041f));
			}	
		}
        while (it < 45){
			foreach (GameObject orb in camOrbs){
				xFloat = UnityEngine.Random.Range(-0.0004f, 0.0004f);
				zFloat = UnityEngine.Random.Range(-0.0004f, 0.0004f);
				yield return new WaitForSeconds(0.01f);
                float yRot = UnityEngine.Random.Range(-15f, 15f);
				orb.transform.localPosition = new 
					Vector3(orb.transform.localPosition.x + xFloat, 
					orb.transform.localPosition.y, 
					orb.transform.localPosition.z + zFloat);
                    orb.transform.localEulerAngles = new Vector3(orb.transform.localEulerAngles.x, orb.transform.localEulerAngles.y + yRot, orb.transform.localEulerAngles.z);
			}
			it++;
		} 
        roomPic.material.color = camColor;
        cooldown = false;
		flashLight.SetActive(true);
		uNav.gameObject.SetActive(true);
		dNav.gameObject.SetActive(true);
		foreach (GameObject orb in camOrbs){orb.SetActive(false);}
		yield break;
	}

    void DotEv(){
		if (_roomOptions[_roomIndex] == "Closet"){return;}
		else{StartCoroutine(Dots());}
    }

	IEnumerator Dots(){
		cooldown = true;
		uNav.gameObject.SetActive(false);
		dNav.gameObject.SetActive(false);
		yield return new WaitForSeconds(0.5f);
        while (flickering ==true){yield return null;}
        flashLight.SetActive(false);
		int i = 0;
		dots.transform.localPosition = new Vector3(-0.32f, 0.42f, 0.0f);
        int dud = UnityEngine.Random.Range(0,100);
		if (inFavRoom == true && correctEvidence[2] == true && dud <=60){
			dots.GetComponent<MeshRenderer>().material = dotMats[correctCol];
			
		}
		else{
			int dotIndex = UnityEngine.Random.Range(0, dotMats.Length);
			while (dotIndex == correctCol){
				dotIndex = UnityEngine.Random.Range(0, dotMats.Length);
			}
			dots.GetComponent<MeshRenderer>().material = dotMats[dotIndex];
		}
        dots.SetActive(true);
        xFloat = UnityEngine.Random.Range(-0.008f, 0.008f);
		zFloat = UnityEngine.Random.Range(-0.008f, 0.008f);
        float yRot = UnityEngine.Random.Range(-0.2f, 0.2f);
		while (i < 70) {
			yield return new WaitForSeconds(0.02f);
			xFloat += UnityEngine.Random.Range(-0.0008f, 0.0008f);
			zFloat += UnityEngine.Random.Range(-0.0008f, 0.0008f);
            yRot += UnityEngine.Random.Range(-0.05f, 0.05f);
			dots.transform.localPosition = new 
				Vector3(dots.transform.localPosition.x + xFloat, 
				dots.transform.localPosition.y, 
				dots.transform.localPosition.z + zFloat);
                dots.transform.localEulerAngles = new Vector3(dots.transform.localEulerAngles.x, dots.transform.localEulerAngles.y + yRot,dots.transform.localEulerAngles.z);
            i++;
		}
		dots.SetActive(false);
		uNav.gameObject.SetActive(true);
		dNav.gameObject.SetActive(true);
		flashLight.SetActive(true);
		cooldown = false;
        yield return new WaitForSeconds (0.3f);
		yield break;
	}

    void EmfEv(){
        if (_roomOptions[_roomIndex] == "Closet"){return;}
		else{StartCoroutine(Emf());}
    }

    IEnumerator Emf(){
        cooldown = true;
		uNav.gameObject.SetActive(false);
		dNav.gameObject.SetActive(false);
		yield return new WaitForSeconds(0.3f);
        while (flickering ==true){yield return null;}
        flashLight.SetActive(false);
        int dud = UnityEngine.Random.Range(0,100);
        if (inFavRoom == true && correctEvidence[3] == true && dud <=60){
            for (int i = 0; i < beepNumber[correctCol]; i++){
            yield return new WaitForSeconds(0.5f);
            Audio.PlaySoundAtTransform("emfbeep", transform);
            }
		}
		else{
			int emfIndex = UnityEngine.Random.Range(0, beepNumber.Length);
			while (emfIndex == correctCol){
				emfIndex = UnityEngine.Random.Range(0, beepNumber.Length);
			}
            for (int i= 0; i < beepNumber[emfIndex]; i++){
            yield return new WaitForSeconds(0.5f);
            Audio.PlaySoundAtTransform("emfbeep", transform);
            }
		}
        yield return new WaitForSeconds(0.5f);
        uNav.gameObject.SetActive(true);
		dNav.gameObject.SetActive(true);
		flashLight.SetActive(true);
		cooldown = false;
		yield break;
    }

    void SpiritEv(){
        if (_roomOptions[_roomIndex] == "Closet"){return;}
		else{StartCoroutine(SpiritBox());}
    }

    IEnumerator SpiritBox(){
        cooldown = true;
		uNav.gameObject.SetActive(false);
		dNav.gameObject.SetActive(false);
		yield return new WaitForSeconds(0.3f);
        while (flickering ==true){yield return null;}
        flashLight.SetActive(false);
        int dud = UnityEngine.Random.Range(0,100);
        if (inFavRoom == true && correctEvidence[4] == true && dud <=60){
			Audio.PlaySoundAtTransform(boxSounds[correctCol], transform);
            
		}
		else{
			int boxIndex = UnityEngine.Random.Range(0, boxSounds.Length);
			while (boxIndex == correctCol){
				boxIndex = UnityEngine.Random.Range(0, boxSounds.Length);
			}
            Audio.PlaySoundAtTransform(boxSounds[boxIndex], transform);
		}
        yield return new WaitForSeconds(1.1f);
		uNav.gameObject.SetActive(true);
		dNav.gameObject.SetActive(true);
		flashLight.SetActive(true);
		cooldown = false;
		yield break;
    }

    void ThermEv(){
        if (_roomOptions[_roomIndex] == "Closet"){return;}
		else{StartCoroutine(SnowFlakes());}
    }

    IEnumerator SnowFlakes(){
        cooldown = true;
		uNav.gameObject.SetActive(false);
		dNav.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        while (flickering ==true){yield return null;}
        flashLight.SetActive(false);
        roomPic.material.color = coldColor; 
        Audio.PlaySoundAtTransform("wind", transform);
        float scaleIndex = UnityEngine.Random.Range(0.3f, 1.2f);
        flake.transform.localScale = new Vector3(2.5f*scaleIndex, 1, 2.5f*scaleIndex);
        flake.transform.localPosition = new Vector3(UnityEngine.Random.Range(-3.6f, 3.0f), flake.transform.localPosition.y, UnityEngine.Random.Range(-3.3f, 3.4f));
        int dud = UnityEngine.Random.Range(0,100);
        if (inFavRoom == true && correctEvidence[5] == true && dud <=60){
            flake.GetComponent<MeshRenderer>().material = flakeMats[correctCol];
        }
        else{
            int flakeIndex = UnityEngine.Random.Range(0, flakeMats.Length);
            while (flakeIndex == correctCol){
                flakeIndex = UnityEngine.Random.Range(0, flakeMats.Length);
            }
            flake.GetComponent<MeshRenderer>().material = flakeMats[flakeIndex];
        }
        flake.SetActive(true);
        int it = 0;
        float yRot = UnityEngine.Random.Range(-1f, 1f);
        while(it < 230){
            yield return null;
            flake.transform.localEulerAngles = new Vector3(flake.transform.localEulerAngles.x, flake.transform.localEulerAngles.y + yRot, flake.transform.localEulerAngles.z);
            it++;
        }
        flake.SetActive(false);
        roomPic.material.color = camColor; 
        yield return new WaitForSeconds (0.3f);
		uNav.gameObject.SetActive(true);
		dNav.gameObject.SetActive(true);
		flashLight.SetActive(true);
		cooldown = false;
		yield break;
    }   

    void UvEv(){
        if (_roomOptions[_roomIndex] == "Closet"){return;}
		else{StartCoroutine(UV());}
    }

    IEnumerator UV(){//UV = 814FFFFF
        cooldown = true;
		uNav.gameObject.SetActive(false);
		dNav.gameObject.SetActive(false);
        yield return new WaitForSeconds(0.5f);
        while (flickering ==true){yield return null;}
        flashLight.SetActive(false);
        uvLightColor = new Color32(0x81, 0x4F, 0xFF, 0xFF);
        flashLight.GetComponent<Light>().color = uvLightColor;
        flashLight.GetComponent<Light>().intensity = 35f;
        flashLight.GetComponent<Light>().spotAngle = 37.2f;
        flashLight.SetActive(true);
        int dud = UnityEngine.Random.Range(0,100);
        if (inFavRoom == true && correctEvidence[6] == true && dud <=60){
            uv.GetComponent<MeshRenderer>().material = uvMats[correctCol];
        }
        else{
            int uvIndex = UnityEngine.Random.Range(0, uvMats.Length);
            while (uvIndex == correctCol){
                uvIndex = UnityEngine.Random.Range(0, uvMats.Length);
            }
            uv.GetComponent<MeshRenderer>().material = uvMats[uvIndex];
        }
        yield return new WaitForSeconds(0.6f);
        uv.SetActive(true);
        uv.transform.localEulerAngles = new Vector3(uv.transform.localEulerAngles.x, UnityEngine.Random.Range(-180, 180), uv.transform.localEulerAngles.z);
        yield return new WaitForSeconds(1.2f);
        uv.SetActive(false);
		uNav.gameObject.SetActive(true);
		dNav.gameObject.SetActive(true);
        flashLight.GetComponent<Light>().color = flashLightColor; 
        flashLight.GetComponent<Light>().intensity = 60f;
        flashLight.GetComponent<Light>().spotAngle = 27.2f;
		cooldown = false;
        yield break;
    }

    void FindColumn(){
        if (bomb.IsIndicatorOn("BOB")){_correctCol = 0;}
        else if(bomb.GetBatteryCount() >= 2 && bomb.GetPortCount() < 3){_correctCol = 1;}
        else if(bomb.GetSerialNumber()[5] %2 == 1){_correctCol = 2;}
        else {_correctCol = 3;}

        //0 Book, 1 Cam, 2 Dots, 3 EMF, 4 SB, 5 Thermo, 6 UV
        switch(locIndex){
            case 0: 
                favToolOptions = new int[]{5, 3, 1, 2};
                favToolIndex = favToolOptions[_correctCol]; 
                break; 
            case 1:
                favToolOptions = new int[]{4, 0, 5, 6};
                favToolIndex = favToolOptions[_correctCol]; 
                break; 
            case 2:
                favToolOptions = new int[]{3, 2, 6, 1};
                favToolIndex = favToolOptions[_correctCol]; 
                break; 
            case 3:
                favToolOptions = new int[]{0, 5, 2, 4};
                favToolIndex = favToolOptions[_correctCol]; 
                break; 
            case 4: 
                favToolOptions = new int[]{6, 5, 3, 0};
                favToolIndex = favToolOptions[_correctCol]; 
                break; 
            case 5:
                favToolOptions = new int[]{2, 1, 0, 5};
                favToolIndex = favToolOptions[_correctCol]; 
                break; 
        }
        Debug.LogFormat("Phasmophobia {0}: The tool used to find the favorite room is the {1}.", moduleId, toolMats[favToolIndex].name);

        if (bomb.GetIndicators().Count() < 2 && locIndex != 0 && locIndex != 4) { correctCol = 0; }
        else if (bomb.GetPortPlates().Count() > 1 && locIndex != 1 && locIndex != 5) { correctCol = 1; }
        else if (bomb.GetBatteryCount() % 2 == 0) { correctCol = 2; }
        else { correctCol = 3; }
		Debug.LogFormat("Phasmophobia #{0}: The correct condition for Table 2 [L>R indexed at 1] is #{1}", moduleId, correctCol+1);
    }

    IEnumerator SanityOverTime(){
        StopCoroutine(Hunt());     
        while (sanity <= 100 && sanity >= 0){
            double chanceToHunt;
            while (roomIndex > -1){
                int sanDec = UnityEngine.Random.Range(0, 3);
                while (sanity - sanDec < 0){sanDec = sanity;}
                sanity -= sanDec; 
                sanityMeter.text = sanity.ToString() + "%";
                chanceToHunt = 20-(0.25*sanity);
                double huntCheck = UnityEngine.Random.Range(0, 100);
                if (huntCheck < chanceToHunt && buffer == false){
                    StartCoroutine(Hunt()); 
                    yield break; 
                }
                yield return new WaitForSeconds(1);
            }
            while (roomIndex == -2){
                int sanInc = 2;
                if (sanity + sanInc > 100){
                    sanInc = 100-sanity; 
                }
                sanity += sanInc; 
                sanityMeter.text = sanity.ToString() + "%";
                yield return new WaitForSeconds(0.5f);
            } 
            yield return null;
        }
        yield return null; 
    }

    IEnumerator Hunt(){
        huntNumber++; 
        Debug.LogFormat("Phasmophobia #{0}: Began Hunt #{1}", moduleId, huntNumber);
        StopCoroutine(SanityOverTime());
        hunting = true; 
        StartCoroutine(Heartbeat());
        int it = 0; 
        while (it<Math.Max(20, 34-2*huntNumber)){
            while (roomIndex==0){
                dNav.gameObject.SetActive(false);
                break; 
            }
            flashLight.SetActive(false); 
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.05f, 0.20f)); 
            flashLight.SetActive(true); 
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.05f, 0.20f)); 
            it++; 
        }
        hunting = false; 
        if (inCloset == false || uLit == true || dLit == true){
            StartCoroutine(Strike()); 
            Debug.LogFormat("Phasmophobia #{0}: You did not hide in the closet or turn off your tools in time.", moduleId);
            yield break; 
        }
        else {  
            StartCoroutine(Buffer());
            yield return null; 
            StartCoroutine(SanityOverTime());
            yield break; 
        }
        
    }

    IEnumerator Heartbeat(){
        while (hunting == true){
            Audio.PlaySoundAtTransform("heart1", transform);
            yield return new WaitForSeconds(1.0f);
        }
        yield break; 
    }

    IEnumerator Buffer(){
        buffer = true; 
        int bufferNum = Math.Max(5, 16-2*huntNumber);
        yield return new WaitForSeconds(bufferNum);
        buffer = false; 
        yield break; 
    }

    IEnumerator FavRoomFlicker(){
        flickering = true; 
        yield return new WaitForSeconds(0.1f);
        flashLight.gameObject.SetActive(false); 
        yield return new WaitForSeconds(0.2f);
        flashLight.gameObject.SetActive(true); 
        yield return new WaitForSeconds(0.3f);
        flashLight.gameObject.SetActive(false); 
        yield return new WaitForSeconds(0.2f);
        flashLight.gameObject.SetActive(true); 
        yield return new WaitForSeconds(0.2f); 
        flickering = false; 
        yield break; 
    }
}
