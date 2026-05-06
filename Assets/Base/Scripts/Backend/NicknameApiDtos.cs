using System;

[Serializable]
public class UpdateNickRequest
{
    public string nick;
}

[Serializable]
public class UpdateNickResponse
{
    public string nick;
    public int raceCoinsBalance;
    public int nickChangePrice;
}

[Serializable]
public class ApiErrorResponse
{
    public string code;
    public string message;
}

[Serializable]
public class ApiErrorEnvelope
{
    public ApiErrorResponse error;
}
