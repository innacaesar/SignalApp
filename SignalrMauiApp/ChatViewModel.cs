using Microsoft.AspNetCore.SignalR.Client;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace SignalrMauiApp
{
    public class ChatViewModel : INotifyPropertyChanged
    {
        HubConnection hubConnection;

        public string UserName { get; set; }
        public string Message { get; set; }
        // רשימת ההודעות
        public ObservableCollection<MessageData> Messages { get; } = new();

        // מחכה לשליחת הודעות
        bool isBusy;
        public bool IsBusy
        {
            get => isBusy;
            set
            {
                if (isBusy != value)
                {
                    isBusy = value;
                    OnPropertyChanged("IsBusy");
                }
            }
        }
        // האם מחובר לשרת
        bool isConnected;
        public bool IsConnected
        {
            get => isConnected;
            set
            {
                if (isConnected != value)
                {
                    isConnected = value;
                    OnPropertyChanged("IsConnected");
                }
            }
        }
        //  שליחת הודעות
        public Command SendMessageCommand { get; }

        public ChatViewModel()
        {
            // יצירת חיבור
            hubConnection = new HubConnectionBuilder()
                .WithUrl("http://192.168.1.14:5156/chat")
                .Build();

            IsConnected = false;    // ברירת מחדל לא מחובר לשרת
            IsBusy = false;         // שליחת הודעה לא מתבצעת

            SendMessageCommand = new Command(async () => await SendMessage(), () => IsConnected);

            hubConnection.Closed += async (error) =>
            {
                SendLocalMessage(string.Empty, "Connection closed...");
                IsConnected = false;
                await Task.Delay(5000);
                await Connect();
            };

            hubConnection.On<string, string>("Receive", (user, message) =>
            {
                SendLocalMessage(user, message);
            });
        }
        // התחברות לשרת
        public async Task Connect()
        {
            if (IsConnected)
                return;
            try
            {
                await hubConnection.StartAsync();
                SendLocalMessage(string.Empty, "You have joined the chat...");

                IsConnected = true;
            }
            catch (Exception ex)
            {
                SendLocalMessage(string.Empty, $"Error connecting: {ex.Message}");
            }
        }

        // התנתקות מהשרת
        public async Task Disconnect()
        {
            if (!IsConnected) return;

            await hubConnection.StopAsync();
            IsConnected = false;
            SendLocalMessage(string.Empty, "You have left the chat...");
        }

        // שליחת הודעה
        async Task SendMessage()
        {
            try
            {
                IsBusy = true;
                await hubConnection.InvokeAsync("Send", UserName, Message);
            }
            catch (Exception ex)
            {
                SendLocalMessage(string.Empty, $"Error sending message: {ex.Message}");
            }

            IsBusy = false;
        }
        // הוספת הודעה
        private void SendLocalMessage(string user, string message)
        {
            Messages.Insert(0, new MessageData(user, message));
        }
        public event PropertyChangedEventHandler PropertyChanged;
        public void OnPropertyChanged(string prop = "")
        {
            if (PropertyChanged != null)
                PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }

    public record MessageData(string User, string Message);
}
