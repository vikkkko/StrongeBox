using Neo.SmartContract.Framework;
using Neo.SmartContract.Framework.Services.Neo;
using Neo.SmartContract.Framework.Services.System;
using Helper = Neo.SmartContract.Framework.Helper;
using System;
using System.Numerics;

namespace StrongBox
{
    public class Contract1 : SmartContract
    {

        [Appcall("9121e89e8a0849857262d67c8408601b5e8e0524")]
        public static extern object cgasCall(string methid,object[] args);

        static readonly byte[] superAdmin = Helper.ToScriptHash("ALn5rR1iJ7yVWb4dXkTQZkD65nPDp4o8TW");//初始管理員

        public class TransferInfo
        {
            public byte[] from;
            public byte[] to;
            public BigInteger value;
        }

        public static TransferInfo GetTransferInfo(byte[] txid)
        {
            //先验证这个交易有没有被录入过
            StorageMap storageMap_txid = Storage.CurrentContext.CreateMap(nameof(storageMap_txid));
            BigInteger used = storageMap_txid.Get(txid).AsBigInteger();
            if (used == 0)
            {
                var info = cgasCall("getTxInfo", new object[1] { txid });
                return info as TransferInfo;
            }
            TransferInfo transferInfo = new TransferInfo();
            transferInfo.from = new byte[0];
            transferInfo.to = new byte[0];
            transferInfo.value = 0;
            return transferInfo;
        }

        public static void SetDiscount(byte[] who , BigInteger discount)
        {
            StorageMap storageMap_discount = Storage.CurrentContext.CreateMap(nameof(storageMap_discount));
            storageMap_discount.Put(who, discount);
        }

        public static BigInteger GetDiscount(byte[] who)
        {
            StorageMap storageMap_discount = Storage.CurrentContext.CreateMap(nameof(storageMap_discount));
            BigInteger discount =  storageMap_discount.Get(who).AsBigInteger();
            if (discount == 0)
                discount = 10;
            return discount;
        }

        public static object Main(string method ,object[] args)
        {
            if (Runtime.Trigger == TriggerType.Verification)
            {
                return false;
            }
            else if (Runtime.Trigger == TriggerType.Application)
            {
                if (method == "setDiscount")
                {
                    byte[] who = (byte[])args[0];
                    BigInteger discount = (BigInteger)args[1];
                    if (who.Length != 20)
                        return false;
                    if (discount > 10||discount<0)
                        return false;
                    SetDiscount(who,discount);
                    return true;
                }
                if (method == "getDiscount")
                {
                    byte[] who = (byte[])args[0];
                    if (who.Length != 20)
                        return false;
                    BigInteger discount =  GetDiscount(who);
                    return discount;
                }
                if (method == "saveData")
                {
                    if (args.Length != 3)
                        return false;
                    string name = (string)args[0];
                    string data = (string)args[1];
                    byte[] txid = (byte[])args[2];
                    //var txid = (ExecutionEngine.ScriptContainer as Transaction).Hash;
                    TransferInfo transferInfo = GetTransferInfo(txid);
                    if (transferInfo.value == 0)
                        return false;
                    if (transferInfo.to.AsBigInteger() == superAdmin.AsBigInteger())
                    {
                        //查看用户的折扣
                        BigInteger discount = GetDiscount(transferInfo.from);
                        //用户要给的手续费
                        BigInteger value = discount; // (10 * discount) / 10
                        if (value != transferInfo.value)
                            return false;

                        StorageMap storageMap_data = Storage.CurrentContext.CreateMap(nameof(storageMap_data));
                        storageMap_data.Put(name, data);
                    }
                }
                if (method == "getData")
                {
                    string name = (string)args[0];
                    StorageMap storageMap_data = Storage.CurrentContext.CreateMap(nameof(storageMap_data));
                    return storageMap_data.Get(name);
                }
            }
            return false;
        }
    }
}
