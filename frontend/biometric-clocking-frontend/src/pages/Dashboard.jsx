function Dashboard() {

    const role = localStorage.getItem("role");

    return (

        <div style={{padding:"40px"}}>

            <h1>Dashboard</h1>

            <h2>Welcome!</h2>

            <p>Your role is: {role}</p>

        </div>

    );

}

export default Dashboard;