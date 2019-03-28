using ClientSprotoType;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class PickCharacterManager : MonoBehaviour
{
    // 角色信息展示UI
    public GameObject CharacterShowCanvas;

    // 角色名对应的唯一ID
    Dictionary<string, Int64> Characters = new Dictionary<string, Int64> { };
    
    // Use this for initialization
    void Start () {
        Vector3[] pos = new Vector3[3];
        pos[0] = new Vector3(-600, 37, 0);
        pos[1] = new Vector3(0, 37, 0);
        pos[2] = new Vector3(600, 37, 0);

        int index = 0;
        foreach (var item in PlayerInfo.Instance.CharacterList)
        {
            Characters[item.Value.name] = item.Key;
            NewCharacterShowCanvas(item.Value, pos[index]);
            index += 1;
        }
	}
	
    void NewCharacterShowCanvas(character_overview character, Vector3 pos)
    {
        // 生成角色信息UI
        GameObject obj = Instantiate(CharacterShowCanvas);
        Image img = obj.GetComponentInChildren<Image>();
        var rt = img.gameObject.GetComponent<RectTransform>();
        rt.anchoredPosition3D = new Vector3(pos.x, pos.y, pos.z);
        Text nameText = img.transform.Find("NameText").GetComponent<Text>();
        Text jobText = img.transform.Find("JobText").GetComponent<Text>();
        Text sexText = img.transform.Find("SexText").GetComponent<Text>();
        nameText.text = character.name;
        jobText.text = character.job.ToString();
        sexText.text = character.sex.ToString();

        // 为UI添加点击事件
        UnityAction<BaseEventData> click = new UnityAction<BaseEventData>(SelectCharacter);
        EventTrigger.Entry myclick = new EventTrigger.Entry();
        myclick.eventID = EventTriggerType.PointerClick;
        myclick.callback.AddListener(click);
        EventTrigger et = img.GetComponentInChildren<EventTrigger>();
        et.triggers.Add(myclick);

        // 显示
        obj.SetActive(true);
    }

    void SelectCharacter(BaseEventData data)
    {
        GameObject obj = data.selectedObject;
        // 角色信息UI发生点击事件的时候,根据名称设置当前选择的角色唯一ID
        Image img = obj.GetComponentInChildren<Image>();
        Text nameText = img.transform.Find("NameText").GetComponent<Text>();
        Text jobText = img.transform.Find("JobText").GetComponent<Text>();
        Text sexText = img.transform.Find("SexText").GetComponent<Text>();

        PlayerInfo.Instance.UUID = Characters[nameText.text];
        PlayerInfo.Instance.Name = nameText.text;
        PlayerInfo.Instance.Job = Convert.ToInt32(jobText.text);
        PlayerInfo.Instance.Sex = Convert.ToInt32(sexText.text);
    }

    public void EnterGame()
    {
        characterpick.request req = new characterpick.request();
        req.uuid = PlayerInfo.Instance.UUID;
        NetSender.Send<ClientProtocol.characterpick>(req, (_) =>
        {
            characterpick.response rsp = _ as characterpick.response;
            Debug.Log("选择角色结果：" + rsp.ok);
            LoadScene();
        });
        Debug.Log("选择角色唯一ID：" + PlayerInfo.Instance.UUID);
    }

    public void CreateCharacter()
    {
        SceneManager.LoadScene("CreateCharacter");
    }

    void LoadScene()
    {
        SceneManager.LoadScene("MainMap");
    }
}
