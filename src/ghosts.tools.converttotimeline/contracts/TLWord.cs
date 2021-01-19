namespace ghosts.tools.converttotimeline.contracts
{
    public static class TLWord
    {
        public static string TLWordObj { get; } = @" 

        ""HandlerType"": ""Word"",
        ""Initial"": """",
        ""UtcTimeOn"": ""00:00:00"",
        ""UtcTimeOff"": ""24:00:00"",
        ""Loop"": ""true"",
        ""TimeLineEvents"":[
            ""Command"":""create"",
            ""CommandArgs"":[ ""% homedrive %% homepath %\\Documents"" ],
            ""DelayAfter"":""900000"",
            ""DelayBefore"":""0"",
        ]
        ";

        
    }
   
}
