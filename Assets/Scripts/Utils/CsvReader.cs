using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class CsvReader
{
    public int colNum;
    public int rowNum;
    public List<string> colHeads;
    public string[,] data;
    public bool dataIsOk = false;

    public int ReadCSVWithHeadLine(string csvPath)
    {
        dataIsOk = false;
        colNum = 0;
        rowNum = 0;
        string[] lines = System.IO.File.ReadAllLines(csvPath);
        if(lines.Length>0)
        {
            rowNum = lines.Length - 1;
            Debug.Log("CSV File Read success with row="+rowNum);
        }else{
            Debug.Log("CSV File Read failed with no row");
            return -1;
        }
        //deal with head
        colHeads = new List<string>();
        int currentColNum = 0;
        string currentColName = "";
        for (int i = 0; i < lines[0].Length;i++)
        {
            if(lines[0][i]==',')
            {
                colHeads.Add(currentColName);
                currentColName = "";
            }else{
                currentColName += lines[0][i];
            }
        }
        if(currentColName!="")
            colHeads.Add(currentColName);
        if(colHeads.Count>0)
        {
            colNum = colHeads.Count;
            Debug.Log("CSV Head Read Success with colNum="+colNum);
        }else{
            Debug.Log("CSV Head Read faileed with colNum=" + colNum);
            return -1;
        }
        //deal with body
        data = new string[rowNum,colNum];
        for (int row = 0; row < rowNum;row++)
        {
            bool quotationMode = false;
            string rowString = lines[row + 1];
            currentColNum = 0;
            string currentStr = "";
            for (int i = 0; i < rowString.Length; i++)
            {
                if (rowString[i] == '"')
                {
                    quotationMode = !quotationMode;
                    continue;
                }
                if(rowString[i] == ','&&!quotationMode)
                {
                    if(currentColNum>=rowNum)
                    {
                        Debug.Log("ColNum Error");
                    }
                    //Debug.Log("r=" + row + "  c=" + currentColNum);
                    data[row, currentColNum] = currentStr;
                    currentColNum++;
                    currentStr = "";
                }else{
                    currentStr += rowString[i];
                }
            }
            if(currentStr!="")
            {
                data[row, currentColNum] = currentStr;
                currentColNum++;
            }
        }
        return 1;
    }

}

