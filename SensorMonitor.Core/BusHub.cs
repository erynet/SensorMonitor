using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SensorMonitor.Core
{
    #region Message Types / Interfaces

    public interface ISubscriberErrorHandler
    {
        void Handle(IBusMessage message, Exception exception);
    }

    public class DefaultSubscriberErrorHandler : ISubscriberErrorHandler
    {
        public void Handle(IBusMessage message, Exception exception)
        {
            // 기본 동작은 아무것도 안하는 것이다.
        }
    }

    /// <summary>
    /// MessageBus 에 의해 전달될 메시지의 인터페이스.
    /// </summary>
    public interface IBusMessage
    {
        /// <summary>
        /// 메시지의 발송자를 담는다.
        /// </summary>
        object Sender { get; }
    }

    /// <summary>
    /// 발송자에 대한 약한 참조를 제공하는 기본 메시지 클래스
    /// </summary>
    public abstract class BusMessageBase : IBusMessage
    {
        /// <summary>
        /// 발송자에 대한 약한 참조를 저장한다.
        /// </summary>
        private WeakReference _Sender;
        public object Sender
        {
            get
            {
                return _Sender?.Target;
            }
        }

        /// <summary>
        /// MessageBase Class 의 새 객체를 만들고 초기화한다.
        /// </summary>
        /// <param name="sender">메시지 발송자 (보통 "this")</param>
        public BusMessageBase(object sender)
        {
            if (sender == null)
                throw new ArgumentNullException("sender");

            _Sender = new WeakReference(sender);
        }
    }

    /// <summary>
    /// 유저의 데이터가 포함된 메시지
    /// </summary>
    /// <typeparam name="TContent">저장될 내용물의 형식</typeparam>
    public class GenericBusMessage<TContent> : BusMessageBase
    {
        /// <summary>
        /// 메시지의 내용
        /// </summary>
        public TContent Content { get; protected set; }

        /// <summary>
        /// GenericBusMessage 클래스의 새 객체를 생성한다.
        /// </summary>
        /// <param name="sender">메시지 송신자 (보통 "this")</param>
        /// <param name="content">메시지의 내용</param>
        public GenericBusMessage(object sender, TContent content)
            : base(sender)
        {
            Content = content;
        }
    }

    /// <summary>
    /// 기본적인 "취소가능한" 일반 메시지
    /// </summary>
    /// <typeparam name="TContent">저장될 내용물의 형식</typeparam>
    public class CancellableGenericBusMessage<TContent> : BusMessageBase
    {
        /// <summary>
        /// 취소 동작
        /// </summary>
        public Action Cancel { get; protected set; }

        /// <summary>
        /// 메시지의 내용
        /// </summary>
        public TContent Content { get; protected set; }

        /// <summary>
        /// CancellableGenericBusMessage 클래스의 새 객체를 생성한다.
        /// </summary>
        /// <param name="sender">메시지 송신자 (보통 "this")</param>
        /// <param name="content">메시지의 내용</param>
        /// <param name="cancelAction">취소를 위해 호출될 동작</param>
        public CancellableGenericBusMessage(object sender, TContent content, Action cancelAction)
            : base(sender)
        {
            if (cancelAction == null)
                throw new ArgumentNullException("cancelAction");

            Content = content;
            Cancel = cancelAction;
        }
    }

    /// <summary>
    /// 메시지에 대한 구독 내용을 저장하는 토큰
    /// </summary>
    public sealed class BusSubscriptionToken : IDisposable
    {
        private WeakReference _Hub;
        private Type _MessageType;

        /// <summary>
        /// BusSubscriptionToken 클래스의 새 객체를 생성하고 초기화한다.
        /// </summary>
        public BusSubscriptionToken(IBusHub hub, Type messageType)
        {
            if (hub == null)
                throw new ArgumentNullException("hub");

            if (!typeof(IBusMessage).IsAssignableFrom(messageType))
                throw new ArgumentOutOfRangeException("messageType");

            _Hub = new WeakReference(hub);
            _MessageType = messageType;
        }

        /// <summary>
        /// BusSubscriptionToken 클래스의 객체를 폐기한다.
        /// Unscribe 와 같은 효과를 낸다.
        /// </summary>
        public void Dispose()
        {
            if (_Hub.IsAlive)
            {
                var hub = _Hub.Target as IBusHub;

                if (hub != null)
                {
                    var unsubscribeMethod = typeof(IBusHub).GetMethod("Unsubscribe",
                        new Type[] { typeof(BusSubscriptionToken) });
                    unsubscribeMethod = unsubscribeMethod.MakeGenericMethod(_MessageType);
                    unsubscribeMethod.Invoke(hub, new object[] { this });
                }
            }

            GC.SuppressFinalize(this);
        }
    }

    /// <summary>
    /// 메시지 에 대한 구독 내용을 저장하는 인터페이스
    /// </summary>
    public interface IBusSubscription
    {
        /// <summary>
        /// 이 구독에 대한 내용을 담은 토큰을 반환한다.
        /// </summary>
        BusSubscriptionToken SubscriptionToken { get; }

        /// <summary>
        /// 전달이 이루어저야 할지를 판단한다.
        /// </summary>
        /// <param name="message">전달되어야 하는 메시지</param>
        /// <returns>True - 전송, False - 전송되면 안됨</returns>
        bool ShouldAttemptDelivery(IBusMessage message);

        /// <summary>
        /// 메시지를 전달한다.
        /// </summary>
        /// <param name="message">전달되어야 하는 메시지</param>
        void Deliver(IBusMessage message);
    }

    /// <summary>
    /// 메시지 프록시를 정의하는 인터페이스
    /// 
    /// 메시지 프록시는 메시지를 중간에 가로채거나 대체하는데 사용될 수 있다.
    /// </summary>
    public interface IBusProxy
    {
        void Deliver(IBusMessage message, IBusSubscription subscription);
    }

    /// <summary>
    /// 기본적인 "통과" 프록시이다.
    /// 
    /// 메시지를 전달하는것 외에 아무것도 하지 않는다.
    /// </summary>
    public sealed class DefaultBusProxy : IBusProxy
    {
        /// <summary>
        /// 메시지 프록시의 객체를 싱글톤으로 구현하기 위해 사용되는 변수
        /// </summary>
        private static readonly DefaultBusProxy _Instance = new DefaultBusProxy();

        static DefaultBusProxy()
        {
        }

        /// <summary>
        /// 프록시의 싱글톤 변수
        /// </summary>
        public static DefaultBusProxy Instance
        {
            get
            {
                return _Instance;
            }
        }

        private DefaultBusProxy()
        {
        }

        public void Deliver(IBusMessage message, IBusSubscription subscription)
        {
            subscription.Deliver(message);
        }
    }
    #endregion

    #region Exceptions
    /// <summary>
    /// 특정 메시지 형식을 구독하는 도중 에러가 발생하면 Throw 된다.
    /// </summary>
    public class TinyMessengerSubscriptionException : Exception
    {
        private const string ERROR_TEXT = "Unable to add subscription for {0} : {1}";

        public TinyMessengerSubscriptionException(Type messageType, string reason)
            : base(string.Format(ERROR_TEXT, messageType, reason))
        {

        }

        public TinyMessengerSubscriptionException(Type messageType, string reason, Exception innerException)
            : base(string.Format(ERROR_TEXT, messageType, reason), innerException)
        {

        }
    }
    #endregion

    #region Hub Interface
    /// <summary>
    /// 구독/발행 요청을 처리하고 메시지를 전달하는 역할을 하는 BusHub 의 인터페이스
    /// </summary>
    public interface IBusHub
    {
        /// <summary>
        /// 구독하는 메시지 형식에 따라 메시지의 배달 주소가 결정된다.
        /// 모든 참조는 약한 참조로 이루어져 있다.
        /// </summary>
        /// <typeparam name="TMessage">메시지의 형식</typeparam>
        /// <param name="deliveryAction">메시지가 배달되었을때 실행될 동작</param>
        /// <returns>구독해지를 위해 사용될 BusSubscriptionToken</returns>
        BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction)
            where TMessage : class, IBusMessage;

        /// <summary>
        /// 구독하는 메시지 형식에 따라 메시지의 배달 주소가 결정된다.
        /// 메시지는 모두 특정 프록시를 통해 전달된다.
        /// 모든 참조는 약한 참조로 이루어져 있다.
        /// </summary>
        /// <typeparam name="TMessage">메시지의 형식</typeparam>
        /// <param name="deliveryAction">메시지가 배달되었을때 실행될 동작</param>
        /// <param name="proxy">메시지를 전달하는데 사용될 프록시</param>
        /// <returns>구독해지를 위해 사용될 BusSubscriptionToken</returns>
        BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, IBusProxy proxy)
            where TMessage : class, IBusMessage;

        /// <summary>
        /// 구독하는 메시지 형식에 따라 메시지의 배달 주소가 결정된다.
        /// 강한 참조를 사용할지 여부를 결정할 수 있다.
        /// </summary>
        /// <typeparam name="TMessage">메시지의 형식</typeparam>
        /// <param name="deliveryAction">메시지가 배달되었을때 실행될 동작</param>
        /// <param name="useStrongReferences">deliveryAction 으로 메시지를 전달할때 강한 참조를 사용할지 여부</param>
        /// <returns>구독해지를 위해 사용될 BusSubscriptionToken</returns>
        BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, bool useStrongReferences)
            where TMessage : class, IBusMessage;

        /// <summary>
        /// 구독하는 메시지 형식에 따라 메시지의 배달 주소가 결정된다.
        /// 메시지는 모두 특정 프록시를 통해 전달된다.
        /// 강한 참조를 사용할지 여부를 결정할 수 있다.
        /// </summary>
        /// <typeparam name="TMessage">메시지의 형식</typeparam>
        /// <param name="deliveryAction">메시지가 배달되었을때 실행될 동작</param>
        /// <param name="proxy">메시지를 전달하는데 사용될 프록시</param>
        /// <param name="useStrongReferences">deliveryAction 으로 메시지를 전달할때 강한 참조를 사용할지 여부</param>
        /// <returns>구독해지를 위해 사용될 BusSubscriptionToken</returns>

        BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, bool useStrongReferences, IBusProxy proxy)
            where TMessage : class, IBusMessage;

        /// <summary>
        /// 구독하는 메시지 형식에 따라 메시지의 배달 주소가 결정된다.
        /// 모든 참조는 약한 참조로 이루어져 있다.
        /// </summary>
        /// <typeparam name="TMessage">메시지의 형식</typeparam>
        /// <param name="deliveryAction">메시지가 배달되었을때 실행될 동작</param>
        /// <param name="messageFilter">메시지 내용에 의거한 2차 필터 함수를 지정</param>
        /// <returns>구독해지를 위해 사용될 BusSubscriptionToken</returns>
        BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter)
            where TMessage : class, IBusMessage;

        /// <summary>
        /// 구독하는 메시지 형식에 따라 메시지의 배달 주소가 결정된다.
        /// 메시지는 모두 특정 프록시를 통해 전달된다.
        /// 모든 참조는 약한 참조로 이루어져 있다.
        /// </summary>
        /// <typeparam name="TMessage">메시지의 형식</typeparam>
        /// <param name="deliveryAction">메시지가 배달되었을때 실행될 동작</param>
        /// <param name="messageFilter">메시지 내용에 의거한 2차 필터 함수를 지정</param>
        /// <param name="proxy">메시지를 전달하는데 사용될 프록시</param>
        /// <returns>구독해지를 위해 사용될 BusSubscriptionToken</returns>
        BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter, IBusProxy proxy)
            where TMessage : class, IBusMessage;

        /// <summary>
        /// 구독하는 메시지 형식에 따라 메시지의 배달 주소가 결정된다.
        /// 강한 참조를 사용할지 여부를 결정할 수 있다.
        /// </summary>
        /// <typeparam name="TMessage">메시지의 형식</typeparam>
        /// <param name="deliveryAction">메시지가 배달되었을때 실행될 동작</param>
        /// <param name="messageFilter">메시지 내용에 의거한 2차 필터 함수를 지정</param>
        /// <param name="useStrongReferences">deliveryAction 으로 메시지를 전달할때 강한 참조를 사용할지 여부</param>
        /// <returns>구독해지를 위해 사용될 BusSubscriptionToken</returns>
        BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter, bool useStrongReferences)
            where TMessage : class, IBusMessage;

        /// <summary>
        /// 구독하는 메시지 형식에 따라 메시지의 배달 주소가 결정된다.
        /// 강한 참조를 사용할지 여부를 결정할 수 있다.
        /// </summary>
        /// <typeparam name="TMessage">메시지의 형식</typeparam>
        /// <param name="deliveryAction">메시지가 배달되었을때 실행될 동작</param>
        /// <param name="messageFilter">메시지 내용에 의거한 2차 필터 함수를 지정</param>
        /// <param name="useStrongReferences">deliveryAction 으로 메시지를 전달할때 강한 참조를 사용할지 여부</param>
        /// /// <param name="proxy">메시지를 전달하는데 사용될 프록시</param>
        /// <returns>구독해지를 위해 사용될 BusSubscriptionToken</returns>
        BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter, bool useStrongReferences, IBusProxy proxy)
            where TMessage : class, IBusMessage;

        /// <summary>
        /// 특정 메시지 형식을 구독해지 한다.
        /// 특정 메시지 형식이 존재하지 않더라도 예외를 발생시키지 않는다.
        /// </summary>
        /// <typeparam name="TMessage">메시지의 형식</typeparam>
        /// <param name="subscriptionToken">구독의 결과로 얻은 BusSubscriptionToken 객체</param>
        void Unsubscribe<TMessage>(BusSubscriptionToken subscriptionToken) where TMessage : class, IBusMessage;

        /// <summary>
        /// 특정 구독자에 대해 메시지를 발행한다.
        /// </summary>
        /// <typeparam name="TMessage">메시지의 형식</typeparam>
        /// <param name="message">전달될 메시지</param>
        void Publish<TMessage>(TMessage message) where TMessage : class, IBusMessage;

        /// <summary>
        /// 특정 구독자에 대해 메시지를 비동기적으로 발행한다.
        /// </summary>
        /// <typeparam name="TMessage">메시지의 형식</typeparam>
        /// <param name="message">전달될 메시지</param>
        void PublishAsync<TMessage>(TMessage message) where TMessage : class, IBusMessage;

        /// <summary>
        /// 특정 구독자에 대해 메시지를 비동기적으로 발행한다.
        /// </summary>
        /// <typeparam name="TMessage">메시지의 형식</typeparam>
        /// <param name="message">전달될 메시지</param>
        /// <param name="callback">완료시에 비동기적으로 호출될 콜백 함수</param>
        void PublishAsync<TMessage>(TMessage message, AsyncCallback callback) where TMessage : class, IBusMessage;
    }
    #endregion

    #region Hub Implementation
    /// <summary>
    /// 구독/발행 요청을 처리하고 메시지를 전달하는 역할을 하는 BusHub 의 구현체
    /// </summary>
    public sealed class BusHub : IBusHub
    {
        readonly ISubscriberErrorHandler _SubscriberErrorHandler;

        #region ctor methods

        public BusHub()
        {
            _SubscriberErrorHandler = new DefaultSubscriberErrorHandler();
        }

        public BusHub(ISubscriberErrorHandler subscriberErrorHandler)
        {
            _SubscriberErrorHandler = subscriberErrorHandler;
        }
        #endregion

        #region Private Types and Interfaces
        private class WeakBusSubscription<TMessage> : IBusSubscription
            where TMessage : class, IBusMessage
        {
            protected BusSubscriptionToken _SubscriptionToken;
            protected WeakReference _DeliveryAction;
            protected WeakReference _MessageFilter;

            public BusSubscriptionToken SubscriptionToken
            {
                get { return _SubscriptionToken; }
            }

            public bool ShouldAttemptDelivery(IBusMessage message)
            {
                if (message == null)
                    return false;

                //if (!(typeof(TMessage).IsAssignableFrom(message.GetType())))
                if (!(message is TMessage))
                    return false;

                if (!_DeliveryAction.IsAlive)
                    return false;

                if (!_MessageFilter.IsAlive)
                    return false;

                return ((Func<TMessage, bool>)_MessageFilter.Target).Invoke(message as TMessage);
            }

            public void Deliver(IBusMessage message)
            {
                if (!(message is TMessage))
                    throw new ArgumentException("Message is not the correct type");

                if (!_DeliveryAction.IsAlive)
                    return;

                ((Action<TMessage>)_DeliveryAction.Target).Invoke(message as TMessage);
            }

            public WeakBusSubscription(BusSubscriptionToken subscriptionToken, Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter)
            {
                if (subscriptionToken == null)
                    throw new ArgumentNullException("subscriptionToken");

                if (deliveryAction == null)
                    throw new ArgumentNullException("deliveryAction");

                if (messageFilter == null)
                    throw new ArgumentNullException("messageFilter");

                _SubscriptionToken = subscriptionToken;
                _DeliveryAction = new WeakReference(deliveryAction);
                _MessageFilter = new WeakReference(messageFilter);
            }
        }

        private class StrongBusSubscription<TMessage> : IBusSubscription
            where TMessage : class, IBusMessage
        {
            protected BusSubscriptionToken _SubscriptionToken;
            protected Action<TMessage> _DeliveryAction;
            protected Func<TMessage, bool> _MessageFilter;

            public BusSubscriptionToken SubscriptionToken
            {
                get { return _SubscriptionToken; }
            }

            public bool ShouldAttemptDelivery(IBusMessage message)
            {
                if (message == null)
                    return false;

                //if (!(typeof(TMessage).IsAssignableFrom(message.GetType())))
                if (!(message is TMessage))
                    return false;

                return _MessageFilter.Invoke(message as TMessage);
            }

            public void Deliver(IBusMessage message)
            {
                if (!(message is TMessage))
                    throw new ArgumentException("Message is not the correct type");

                _DeliveryAction.Invoke(message as TMessage);
            }

            public StrongBusSubscription(BusSubscriptionToken subscriptionToken, Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter)
            {
                if (subscriptionToken == null)
                    throw new ArgumentNullException("subscriptionToken");

                if (deliveryAction == null)
                    throw new ArgumentNullException("deliveryAction");

                if (messageFilter == null)
                    throw new ArgumentNullException("messageFilter");

                _SubscriptionToken = subscriptionToken;
                _DeliveryAction = deliveryAction;
                _MessageFilter = messageFilter;
            }
        }
        #endregion

        #region Subscription dictionary
        private class SubscriptionItem
        {
            public IBusProxy Proxy { get; private set; }
            public IBusSubscription Subscription { get; private set; }

            public SubscriptionItem(IBusProxy proxy, IBusSubscription subscription)
            {
                Proxy = proxy;
                Subscription = subscription;
            }
        }

        private readonly object _SubscriptionsPadlock = new object();
        private readonly List<SubscriptionItem> _Subscriptions = new List<SubscriptionItem>();
        #endregion

        #region Public API

        public BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction)
            where TMessage : class, IBusMessage
        {
            return AddSubscriptionInternal<TMessage>(deliveryAction, (m) => true, true, DefaultBusProxy.Instance);
        }

        public BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, IBusProxy proxy)
            where TMessage : class, IBusMessage
        {
            return AddSubscriptionInternal<TMessage>(deliveryAction, (m) => true, true, proxy);
        }

        public BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, bool useStrongReferences)
            where TMessage : class, IBusMessage
        {
            return AddSubscriptionInternal<TMessage>(deliveryAction, (m) => true, useStrongReferences, DefaultBusProxy.Instance);
        }

        public BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, bool useStrongReferences, IBusProxy proxy)
            where TMessage : class, IBusMessage
        {
            return AddSubscriptionInternal<TMessage>(deliveryAction, (m) => true, useStrongReferences, proxy);
        }

        public BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter)
            where TMessage : class, IBusMessage
        {
            return AddSubscriptionInternal<TMessage>(deliveryAction, messageFilter, true, DefaultBusProxy.Instance);
        }

        public BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter, IBusProxy proxy)
            where TMessage : class, IBusMessage
        {
            return AddSubscriptionInternal<TMessage>(deliveryAction, messageFilter, true, proxy);
        }

        public BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter, bool useStrongReferences)
            where TMessage : class, IBusMessage
        {
            return AddSubscriptionInternal<TMessage>(deliveryAction, messageFilter, useStrongReferences, DefaultBusProxy.Instance);
        }

        public BusSubscriptionToken Subscribe<TMessage>(Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter, bool useStrongReferences, IBusProxy proxy)
            where TMessage : class, IBusMessage
        {
            return AddSubscriptionInternal<TMessage>(deliveryAction, messageFilter, useStrongReferences, proxy);
        }

        public void Unsubscribe<TMessage>(BusSubscriptionToken subscriptionToken)
            where TMessage : class, IBusMessage
        {
            RemoveSubscriptionInternal<TMessage>(subscriptionToken);
        }

        public void Publish<TMessage>(TMessage message)
            where TMessage : class, IBusMessage
        {
            PublishInternal<TMessage>(message);
        }

        public void PublishAsync<TMessage>(TMessage message)
            where TMessage : class, IBusMessage
        {
            PublishAsyncInternal<TMessage>(message, null);
        }

        public void PublishAsync<TMessage>(TMessage message, AsyncCallback callback)
            where TMessage : class, IBusMessage
        {
            PublishAsyncInternal<TMessage>(message, callback);
        }
        #endregion

        #region Internal Methods
        private BusSubscriptionToken AddSubscriptionInternal<TMessage>(Action<TMessage> deliveryAction, Func<TMessage, bool> messageFilter, bool strongReference, IBusProxy proxy)
                where TMessage : class, IBusMessage
        {
            if (deliveryAction == null)
                throw new ArgumentNullException("deliveryAction");

            if (messageFilter == null)
                throw new ArgumentNullException("messageFilter");

            if (proxy == null)
                throw new ArgumentNullException("proxy");


            var subscriptionToken = new BusSubscriptionToken(this, typeof(TMessage));

            IBusSubscription subscription;
            if (strongReference)
                subscription = new StrongBusSubscription<TMessage>(subscriptionToken, deliveryAction, messageFilter);
            else
                subscription = new WeakBusSubscription<TMessage>(subscriptionToken, deliveryAction, messageFilter);

            lock (_SubscriptionsPadlock)
            {
                _Subscriptions.Add(new SubscriptionItem(proxy, subscription));
            }

            return subscriptionToken;

        }

        private void RemoveSubscriptionInternal<TMessage>(BusSubscriptionToken subscriptionToken)
                where TMessage : class, IBusMessage
        {
            if (subscriptionToken == null)
                throw new ArgumentNullException("subscriptionToken");

            lock (_SubscriptionsPadlock)
            {
                var currentlySubscribed = (from sub in _Subscriptions
                                           where object.ReferenceEquals(sub.Subscription.SubscriptionToken, subscriptionToken)
                                           select sub).ToList();

                currentlySubscribed.ForEach(sub => _Subscriptions.Remove(sub));
            }
        }

        private void PublishInternal<TMessage>(TMessage message)
                where TMessage : class, IBusMessage
        {
            if (message == null)
                throw new ArgumentNullException("message");

            List<SubscriptionItem> currentlySubscribed;
            lock (_SubscriptionsPadlock)
            {
                currentlySubscribed = (from sub in _Subscriptions
                                       where sub.Subscription.ShouldAttemptDelivery(message)
                                       select sub).ToList();
            }

            currentlySubscribed.ForEach(sub =>
            {
                try
                {
                    sub.Proxy.Deliver(message, sub.Subscription);
                }
                catch (Exception exception)
                {
                    _SubscriberErrorHandler.Handle(message, exception);
                }
            });
        }

        private void PublishAsyncInternal<TMessage>(TMessage message, AsyncCallback callback)
            where TMessage : class, IBusMessage
        {
            Action publishAction = () => { PublishInternal<TMessage>(message); };

            publishAction.BeginInvoke(callback, null);
        }
        #endregion
    }
    #endregion
}
